using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
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
        Task<IDictionary<int, MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria);
    }

    internal class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IDatabaseFilteringAnalyser databaseFilteringAnalyser;
        private readonly IMatchCriteriaAnalyser matchCriteriaAnalyser;
        private readonly ILogger searchLogger;
        private readonly IPGroupRepository pGroupRepository;

        public DonorMatchingService(
            IActiveRepositoryFactory repositoryFactory,
            IMatchFilteringService matchFilteringService,
            IDatabaseFilteringAnalyser databaseFilteringAnalyser,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            IMatchCriteriaAnalyser matchCriteriaAnalyser)
        {
            donorSearchRepository = repositoryFactory.GetDonorSearchRepository();
            pGroupRepository = repositoryFactory.GetPGroupRepository();
            this.matchFilteringService = matchFilteringService;
            this.databaseFilteringAnalyser = databaseFilteringAnalyser;
            this.searchLogger = searchLogger;
            this.matchCriteriaAnalyser = matchCriteriaAnalyser;
        }

        public async Task<IDictionary<int, MatchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria)
        {
            var orderedLoci = matchCriteriaAnalyser.LociInMatchingOrder(criteria);

            // Keyed by DonorId
            IDictionary<int, MatchResult> results = new Dictionary<int, MatchResult>();

            var firstLocus = orderedLoci.First();
            var firstLocusIterator = MatchIndependentLocus(criteria, firstLocus);

            async IAsyncEnumerable<MatchResult> PipeToLocus(
                IAsyncEnumerable<MatchResult> prevIterator,
                Locus locus,
                AlleleLevelLocusMatchCriteria locusCriteria,
                HashSet<Locus> matchedLoci)
            {
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
                    var iterator = MatchIndependentLocus(criteria, locus);

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
                            var donorIdsToFilter = donorIds.ToHashSet();
                            var newResults = FindMatchesAtLocus(criteria.SearchType, locus, locusCriteria, donorIdsToFilter);
                            await foreach (var locusResult in newResults)
                            {
                                var donorId = locusResult.Item1;
                                if (dict.ContainsKey(donorId))
                                {
                                    var result = dict[donorId];
                                    result.SetMatchDetailsForLocus(locus, locusResult.Item2);
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

            var fullyMatched = orderedLoci
                .Skip(1)
                .Aggregate(
                    (firstLocusIterator, new HashSet<Locus> {orderedLoci.First()}),
                    (aggregator, locus) =>
                        (PipeToLocus(
                                aggregator.Item1,
                                locus,
                                criteria.LocusCriteria.GetLocus(locus),
                                aggregator.Item2),
                            aggregator.Item2.Concat(new HashSet<Locus> {locus}).ToHashSet()
                        )
                );

            await foreach (var result in fullyMatched.Item1.WhereAsync(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria)))
            {
                results[result.DonorId] = result;
            }

            foreach (var result in results)
            {
                result.Value.PopulateMismatches(orderedLoci);
            }

            return results;
        }

        private IAsyncEnumerable<MatchResult> MatchIndependentLocus(AlleleLevelMatchCriteria criteria, Locus locus)
        {
            return FindMatchesAtLocus(criteria.SearchType, locus, criteria.LocusCriteria.GetLocus(locus))
                .SelectAsync(lmd =>
                {
                    var (donorId, locusMatchDetails) = lmd;
                    var matchResult = new MatchResult {DonorId = donorId};
                    matchResult.SetMatchDetailsForLocus(locus, locusMatchDetails);
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
                DonorType = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(repoCriteria) ? searchType : (DonorType?) null,
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