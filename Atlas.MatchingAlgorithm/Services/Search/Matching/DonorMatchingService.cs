using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Helpers;
using Dasync.Collections;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchingService
    {
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci.
        /// Performs filtering against loci and total mismatch counts.
        /// </summary>
        /// <returns>
        /// A dictionary of PotentialSearchResults, keyed by donor id.
        /// MatchDetails will be populated only for the specified loci.
        /// </returns>
        Task<IDictionary<int, MatchResult>> FindMatchesForLoci(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> loci,
            ICollection<Locus> loci2 = null);
    }

    public class DonorMatchingService : DonorMatchingServiceBase, IDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IDatabaseFilteringAnalyser databaseFilteringAnalyser;
        private readonly ILogger searchLogger;
        private readonly IPGroupRepository pGroupRepository;

        public DonorMatchingService(
            IActiveRepositoryFactory repositoryFactory,
            IMatchFilteringService matchFilteringService,
            IDatabaseFilteringAnalyser databaseFilteringAnalyser,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger
        )
        {
            donorSearchRepository = repositoryFactory.GetDonorSearchRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.matchFilteringService = matchFilteringService;
            this.databaseFilteringAnalyser = databaseFilteringAnalyser;
            this.searchLogger = searchLogger;
        }

        public async Task<IDictionary<int, MatchResult>> FindMatchesForLoci(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> loci,
            ICollection<Locus> loci2)
        {
            if (loci.Any(locus => !LocusSettings.LociPossibleToMatchInMatchingPhaseOne.Contains(locus)))
            {
                // Currently the logic here is not advised for these loci
                // Donors can be untyped at these loci, which counts as a potential match
                // so a simple search of the database would return a huge number of donors. 
                // To avoid serialising that many results, we filter on these loci based on the results at other loci
                throw new NotImplementedException();
            }

            var phase1LociMismatchCounts = loci.Select(l => (l, criteria.LocusCriteria.GetLocus(l)))
                .OrderBy(c => c.Item2?.MismatchCount)
                .ToList();

            var phase2LociMismatchCounts = loci2.Select(l => (l, criteria.LocusCriteria.GetLocus(l)))
                .OrderBy(c => c.Item2?.MismatchCount)
                .ToList();

            var lociMismatchCounts = phase1LociMismatchCounts.Concat(phase2LociMismatchCounts).ToList();

            // Keyed by DonorId
            IDictionary<int, MatchResult> results = new Dictionary<int, MatchResult>();

            var firstLocus = lociMismatchCounts.First();
            var firstLocusIterator = MatchIndependentLocus(criteria, firstLocus);

            async IAsyncEnumerable<MatchResult> PipeToLocus(
                IAsyncEnumerable<MatchResult> prevIterator,
                (Locus, AlleleLevelLocusMatchCriteria) locusCriteria,
                HashSet<Locus> matchedLoci)
            {
                var (locus, c) = locusCriteria;

                // If all previous loci have been allowed 2 mismatches, we cannot guarantee that all results are contained in that query 
                var canStartFiltering = matchedLoci.Any(l => criteria.LocusCriteria.GetLocus(l).MismatchCount != 2)
                                        // Matched loci * 2 = number of mismatches there must be to have not been returned by now. If this exceeds the overall limit, we must have found all donors by now.
                                        || matchedLoci.Count * 2 > criteria.DonorMismatchCount;

                // if first locus matches no donors, but is allowed 2 mismatches. E.g. 8/10, 4/8 searches can trigger this.
                // Do this outside of batching to ensure all results are actually grouped if first locus returns > batchsize results.
                // TODO: ATLAS-714: Lots of commonisation probably possible here!
                if (!canStartFiltering)
                {
                    // This will be quite memory intensive! But necessary for these very lenient searches I think.
                    var dict = new Dictionary<int, MatchResult>();
                    await foreach (var result in prevIterator)
                    {
                        dict[result.DonorId] = result;
                    }
                    
                    // TODO: ATLAS-714: Do not create unnecessary match Result objects here
                    var iterator = MatchIndependentLocus(criteria, (locus, c));

                    await foreach (var locusResult in iterator)
                    {
                        var donorId = locusResult.DonorId;
                        if (dict.ContainsKey(donorId))
                        {
                            var result = dict[donorId];
                            result.SetMatchDetailsForLocus(locus, locusResult.MatchDetailsForLocus(locus));
                        }
                        else
                        {
                            var result = new MatchResult {DonorId = donorId};

                            result.SetMatchDetailsForLocus(locus, locusResult.MatchDetails.GetLocus(locus));

                            // Final filtering uses *populated* loci. 
                            // If matching has been performed at a locus, and no results came back for it - it needs to have 0 matches at each of these loci. 
                            foreach (var matchedLocus in matchedLoci)
                            {
                                result.SetMatchDetailsForLocus(matchedLocus, new LocusMatchDetails());
                            }

                            dict.Add(donorId, result);
                        }
                    }
                    
                    foreach (var result in dict)
                    {
                        var allLoci = matchedLoci.Append(locus);
                        if (allLoci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(result.Value, criteria, l)))
                        {
                            yield return result.Value;
                        }
                    }
                }
                else
                {
                    // TODO: ATLAS-714: Make batch size configurable - allow finer control over performance vs. memory 
                    await foreach (var resultBatch in prevIterator.Batch(250_000))
                    {
                        using (searchLogger.RunTimed($"Matching Batch at locus {locus}", LogLevel.Verbose))
                        {
                            var dict = resultBatch.ToDictionary(r => r.DonorId, r => r);
                            var donorIds = dict.Keys;
                            var donorIdsToFilter = canStartFiltering ? donorIds.ToHashSet() : null;
                            var newResults = FindMatchesAtLocus(criteria.SearchType, locus, c, donorIdsToFilter);
                            await foreach (var locusResult in newResults)
                            {
                                var donorId = locusResult.Item1;
                                if (dict.ContainsKey(donorId))
                                {
                                    var result = dict[donorId];
                                    result.SetMatchDetailsForLocus(locus, locusResult.Item2);
                                }
                                else if (canStartFiltering)
                                {
                                    // "CanStartFiltering" means that this code path should never be hit! 
                                    // But if it does, e.g. with an SQL mistake / refactor, we can guarantee that any donor not already matched by now should not be returned, so we don't return these donors
                                }
                                else
                                {
                                    var result = new MatchResult {DonorId = donorId};

                                    result.MatchDetails.SetLocus(locus, locusResult.Item2);
                                
                                    // Final filtering uses *populated* loci. 
                                    // If matching has been performed at a locus, and no results came back for it - it needs to have 0 matches at each of these loci. 
                                    foreach (var matchedLocus in matchedLoci)
                                    {
                                        result.SetMatchDetailsForLocus(matchedLocus, new LocusMatchDetails());
                                    }

                                    dict.Add(donorId, result);
                                }
                            }

                            foreach (var result in dict)
                            {
                                var allLoci = matchedLoci.Append(locus);
                                if (allLoci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(result.Value, criteria, l)))
                                {
                                    yield return result.Value;
                                }
                            }
                        }
                    }
                }
            }

            var fullyMatched = lociMismatchCounts
                .Skip(1)
                .Aggregate((firstLocusIterator, new HashSet<Locus> {lociMismatchCounts.First().l}), (aggregator, locusCriteria) =>
                    (PipeToLocus(aggregator.Item1, locusCriteria, aggregator.Item2),
                        aggregator.Item2.Concat(new HashSet<Locus> {locusCriteria.l}).ToHashSet())
                );

            await foreach (var result in fullyMatched.Item1.WhereAsync(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria)))
            {
                results[result.DonorId] = result;
            }

            var specifiedLoci = lociMismatchCounts.Select(x => x.l).ToList();

            foreach (var result in results)
            {
                result.Value.PopulateMismatches(specifiedLoci);
            }

            return results;
        }

        private IAsyncEnumerable<MatchResult> MatchIndependentLocus(
            AlleleLevelMatchCriteria criteria,
            (Locus l, AlleleLevelLocusMatchCriteria) firstLocus)
        {
            return FindMatchesAtLocus(criteria.SearchType, firstLocus.l, firstLocus.Item2)
                .SelectAsync(lmd =>
                {
                    var (donorId, locusMatchDetails) = lmd;
                    var matchResult = new MatchResult {DonorId = donorId};
                    matchResult.SetMatchDetailsForLocus(firstLocus.l, locusMatchDetails);
                    return matchResult;
                });
        }

        // (donorId, locusResult)
        private async IAsyncEnumerable<(int, LocusMatchDetails)> FindMatchesAtLocus(
            DonorType searchType,
            Locus locus,
            AlleleLevelLocusMatchCriteria criteria,
            HashSet<int> donorIds = null
        )
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchDonorType = searchType,
                PGroupIdsToMatchInPositionOne = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionOne),
                PGroupIdsToMatchInPositionTwo = await pGroupRepository.GetPGroupIds(criteria.PGroupsToMatchInPositionTwo),
                MismatchCount = criteria.MismatchCount,
            };

            var filteringOptions = new MatchingFilteringOptions
            {
                ShouldFilterOnDonorType = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(repoCriteria),
                DonorIds = donorIds
            };

            var relations = donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria, filteringOptions);

            (int, LocusMatchDetails) current = default;

            // requires ordering by donorId in SQL layer
            await foreach (var relation in relations)
            {
                var positionPair = (relation.SearchTypePosition, relation.MatchingTypePosition);
                if (current == default)
                {
                    current = (relation.DonorId, new LocusMatchDetails
                    {
                        PositionPairs = new HashSet<(LocusPosition, LocusPosition)> {positionPair}
                    });
                    continue;
                }

                if (current.Item1 == relation.DonorId)
                {
                    current.Item2.PositionPairs.Add(positionPair);
                }
                else
                {
                    yield return current;
                    current = (relation.DonorId, new LocusMatchDetails
                    {
                        PositionPairs = new HashSet<(LocusPosition, LocusPosition)> {positionPair}
                    });
                }
            }

            // Will still be uninitialised if no results for locus.
            if (current != default)
            {
                yield return current;
            }
        }
    }
}