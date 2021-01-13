using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Settings;
using Dasync.Collections;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchingService
    {
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci.
        /// Performs filtering against loci and total mismatch counts.
        ///
        /// All donors matching the hla-based matching criteria must be returned by this method - it will return a superset of the final search results. 
        /// </summary>
        /// <returns>
        /// A dictionary of PotentialSearchResults, keyed by donor id.
        /// MatchDetails will be populated only for the specified loci.
        /// </returns>
        Task<IAsyncEnumerable<MatchResult>> FindMatchingDonors(AlleleLevelMatchCriteria criteria);
    }

    internal class DonorMatchingService : IDonorMatchingService
    {
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IMatchCriteriaAnalyser matchCriteriaAnalyser;
        private readonly IPerLocusDonorMatchingService perLocusDonorMatchingService;
        private readonly MatchingConfigurationSettings matchingConfigurationSettings;
        private readonly ILogger searchLogger;

        public DonorMatchingService(
            IMatchFilteringService matchFilteringService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            IMatchCriteriaAnalyser matchCriteriaAnalyser,
            IPerLocusDonorMatchingService perLocusDonorMatchingService,
            MatchingConfigurationSettings matchingConfigurationSettings)
        {
            this.matchFilteringService = matchFilteringService;
            this.searchLogger = searchLogger;
            this.matchCriteriaAnalyser = matchCriteriaAnalyser;
            this.perLocusDonorMatchingService = perLocusDonorMatchingService;
            this.matchingConfigurationSettings = matchingConfigurationSettings;
        }

        public async Task<IAsyncEnumerable<MatchResult>> FindMatchingDonors(AlleleLevelMatchCriteria criteria)
        {
            var orderedLoci = matchCriteriaAnalyser.LociInMatchingOrder(criteria);
            searchLogger.SendTrace($"Will match loci in the following order: {orderedLoci.Select(l => l.ToString()).StringJoin(", ")}");

            var resultStream = orderedLoci
                .Aggregate(
                    (null as IAsyncEnumerable<MatchResult>, new HashSet<Locus>()),
                    (aggregator, locus) =>
                    {
                        var (partialResultStream, matchedLoci) = aggregator;

                        return (
                            MatchAtLocus(criteria, locus, matchedLoci, partialResultStream),
                            matchedLoci.Concat(new HashSet<Locus> {locus}).ToHashSet()
                        );
                    }
                ).Item1
                .WhereAsync(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria));

            return resultStream.SelectAsync(r => r.PopulateMismatches(orderedLoci));
        }

        /// <summary>
        /// This method is designed to be chainable across multiple loci.
        ///
        /// Takes a stream of all matching donors at previously matched loci, and performs filtering at the specified locus,
        /// using the existing donor ids to filter the results of later loci if possible.
        /// </summary>
        /// <param name="criteria">Search request matching criteria</param>
        /// <param name="locus">The locus to perform matching at</param>
        /// <param name="matchedLoci"></param>
        /// <param name="previousLociResultStream"></param>
        /// <returns>A stream of all donors that match the per-locus criteria for this and all previously matched loci, and have at least one match at this locus.</returns>
        private async IAsyncEnumerable<MatchResult> MatchAtLocus(
            AlleleLevelMatchCriteria criteria,
            Locus locus,
            ICollection<Locus> matchedLoci,
            IAsyncEnumerable<MatchResult> previousLociResultStream = null)
        {
            using (searchLogger.RunTimed($"Matching at Locus {locus}. (Timing cumulative across loci.)"))
            {
                var locusCriteria = criteria.LocusCriteria.GetLocus(locus);
                if (!CanFilterByDonorIds(criteria, matchedLoci))
                {
                    var results = previousLociResultStream == null
                        // If no previous results stream, this is the first locus to be matched.
                        ? null
                        // Loads all results from previous loci into memory. This is expected to be very memory intensive, and is only necessary for searches that allow two mismatches at all required loci, e.g. 8/10, 4/8
                        // If this still causes OutOfMemory exceptions, we may need to look into Zipping multiple ordered streams from different loci.
                        : (await System.Linq.AsyncEnumerable.ToListAsync(previousLociResultStream)).ToDictionary(x => x.DonorId, x => x);

                    var locusStream = perLocusDonorMatchingService.FindMatchesAtLocus(locus, locusCriteria, criteria.SearchType);

                    var filteredResults = await ConsolidateAndFilterResults(criteria, matchedLoci, locus, locusStream, results);
                    await foreach (var result in filteredResults)
                    {
                        yield return result;
                    }
                }
                else
                {
                    // Batching is implemented, as each SQL query needs a concrete list of filtered IDs, rather than a stream. 
                    // This batch size control a balance between performance and memory footprint - larger batches will lead to a higher memory footprint, but fewer SQL connections (and therefore faster searches)
                    await foreach (var resultBatch in previousLociResultStream.Batch(matchingConfigurationSettings.MatchingBatchSize))
                    {
                        var donorBatch = resultBatch.ToDictionary(r => r.DonorId, r => r);
                        var donorIds = donorBatch.Keys.ToHashSet();

                        var locusBatchStream =
                            perLocusDonorMatchingService.FindMatchesAtLocus(locus, locusCriteria, criteria.SearchType, donorIds);

                        var filteredResults = await ConsolidateAndFilterResults(criteria, matchedLoci, locus, locusBatchStream, donorBatch);
                        await foreach (var result in filteredResults)
                        {
                            yield return result;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when matching has been performed at some, but not all loci.
        ///
        /// Expected to only return false if no loci have been matched - or if first locus matches no donors, but is allowed 2 mismatches. E.g. 8/10, 4/8 searches.
        /// </summary>
        /// <param name="criteria">Search request matching criteria</param>
        /// <param name="matchedLoci">Loci for which matching requests have been performed.</param>
        /// <returns>
        /// A boolean indicating whether it is safe to filter on donor ids.
        /// i.e. whether the matched loci are enough to guarantee a superset of the final result set, for the given matching criteria
        /// </returns>
        private static bool CanFilterByDonorIds(AlleleLevelMatchCriteria criteria, ICollection<Locus> matchedLoci)
        {
            // If any previously matched locus requires at least one match at that locus, we can guarantee that no donors can fulfil that locus' criteria that hasn't already been returned. 
            var anyMatchedLociRequireNoMismatches = matchedLoci.Any(l => criteria.LocusCriteria.GetLocus(l).MismatchCount != 2);

            // Matched loci * 2 = number of mismatches there must be to have not been returned by now. If this exceeds the overall limit, we must have found all donors by now.
            var numberOfGuaranteedMismatchesForAbsentDonors = matchedLoci.Count * 2;

            return anyMatchedLociRequireNoMismatches || numberOfGuaranteedMismatchesForAbsentDonors > criteria.DonorMismatchCount;
        }

        private async Task<IAsyncEnumerable<MatchResult>> ConsolidateAndFilterResults(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> matchedLoci,
            Locus locus,
            IAsyncEnumerable<(int, LocusMatchDetails)> relationEnumerator,
            IDictionary<int, MatchResult> existingResults = null)
        {
            var allLoci = matchedLoci.Append(locus).ToHashSet();
            var mustMatchAtLocus = criteria.LocusCriteria.GetLocus(locus).MismatchCount < 2;

            return ConsolidateResults(locus, relationEnumerator, mustMatchAtLocus, existingResults)
                .WhereAsync(result => allLoci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(result, criteria, l)));
        }

        /// <summary>
        /// Consolidates results by locus.
        /// </summary>
        /// <param name="locus"> The locus in question.</param>
        /// <param name="relationEnumerator">
        /// "Stream" of donors at this locus. Should not be reified, as this may be extremely large for early loci, and cause memory issues.
        /// </param>
        /// <param name="mustMatchAtLocus">
        /// Determines whether or not all existing results should be returned.
        /// If true - any donor that is not returned by <see cref="relationEnumerator"/> *cannot* be included in the final result set, and so can be dropped.
        /// If false - this locus cannot be considered to independently determine whether any donor should be included in the final result set, so all donors from
        /// <see cref="relationEnumerator"/> should be returned - even if they do not match at this locus.
        /// </param>
        /// <param name="existingResults">
        /// Optional.
        /// If null, implies this is the first locus to be searches upon.
        /// If non-null, this is a reified collection of existing donors - as such this should never contain a number of donors greater than the matching batch size.
        ///
        /// This dictionary is modified as part of this method, so the caller must agree to not rely on the contents of the provided dictionary being stable once called.
        /// If this becomes an issue, this method should be refactored to clone the input dictionary, at the expense of requiring higher memory usage while running large searches. 
        /// </param>
        /// <returns>
        /// All results matching at the current loci, with match details populated at that locus.
        /// For results which have been matched at other loci, such matching details are maintained.
        /// If a match is not *required* at this locus, all provided existing results will be returned, with matching details at this locus iff it is a match at this locus.
        /// </returns>
        private static async IAsyncEnumerable<MatchResult> ConsolidateResults(
            Locus locus,
            IAsyncEnumerable<(int, LocusMatchDetails)> relationEnumerator,
            bool mustMatchAtLocus,
            IDictionary<int, MatchResult> existingResults = null)
        {
            await foreach (var (donorId, locusMatchDetails) in relationEnumerator)
            {
                var result = existingResults?.GetValueOrDefault(donorId) ?? new MatchResult(donorId);
                result.SetMatchDetailsForLocus(locus, locusMatchDetails);

                // This is destructive to the original object passed by reference. This is on purpose, to reduce memory footprint.
                existingResults?.Remove(donorId);
                yield return result;
            }

            // If match is not required at locus, all existing results must be returned, not just those that match at this locus.
            // Matches at the current locus have already been returned and removed from the result dictionary, so this process only returns donors that 
            // were previously matched, but don't match at the current locus.
            if (!mustMatchAtLocus && existingResults != null)
            {
                foreach (var result in existingResults)
                {
                    yield return result.Value;
                }
            }
        }
    }
}