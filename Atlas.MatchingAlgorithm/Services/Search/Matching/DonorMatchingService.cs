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
using Dasync.Collections;
using AsyncEnumerable = System.Linq.AsyncEnumerable;

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
        private const string LoggingPrefix = "MATCHING PHASE1: ";

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

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var phase1LociMismatchCounts = loci.Select(l => (l, criteria.LocusCriteria.GetLocus(l)))
                .OrderBy(c => c.Item2?.MismatchCount)
                .ToList();
            if (phase1LociMismatchCounts.All(c => c.Item2?.MismatchCount == 2))
            {
                throw new NotImplementedException("TODO: ATLAS-714: Re-work out how to do 4/8");
            }

            var phase2LociMismatchCounts = loci2.Select(l => (l, criteria.LocusCriteria.GetLocus(l)))
                .OrderBy(c => c.Item2?.MismatchCount)
                .ToList();

            var lociMismatchCounts = phase1LociMismatchCounts.Concat(phase2LociMismatchCounts).ToList();
            
            // Keyed by DonorId
            IDictionary<int, MatchResult> results = new Dictionary<int, MatchResult>();

            var firstLocus = lociMismatchCounts.First();
            var firstLocusIterator = AsyncEnumerable.Select(FindMatchesAtLocus(criteria.SearchType, firstLocus.l, firstLocus.Item2), lmd =>
            {
                var (donorId, locusMatchDetails) = lmd;
                var matchResult = new MatchResult {DonorId = donorId};
                matchResult.SetMatchDetailsForLocus(firstLocus.l, locusMatchDetails);
                return matchResult;
            });

            async IAsyncEnumerable<MatchResult> PipeToLocus(
                IAsyncEnumerable<MatchResult> prevIterator,
                (Locus, AlleleLevelLocusMatchCriteria) locusCriteria,
                HashSet<Locus> matchedLoci)
            {
                var (locus, c) = locusCriteria;

                await foreach (var resultBatch in prevIterator.Batch(100_000))
                {
                    using (searchLogger.RunTimed($"Matching Batch at locus {locus}"))
                    {
                        var dict = resultBatch.ToDictionary(r => r.DonorId, r => r);
                        var donorIds = dict.Keys;
                        var newResults = FindMatchesAtLocus(criteria.SearchType, locus, c, donorIds.ToHashSet());
                        await foreach (var locusResult in newResults)
                        {
                            var donorId = locusResult.Item1;
                            if (dict.ContainsKey(donorId))
                            {
                                var result = dict[donorId];
                                result.SetMatchDetailsForLocus(locus, locusResult.Item2);
                            }
                            // TODO: Filtering!
                            else
                            {
                                var result = new MatchResult {DonorId = donorId};
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

            var fullyMatched = lociMismatchCounts
                .Skip(1)
                .Aggregate((firstLocusIterator, new HashSet<Locus> {lociMismatchCounts.First().l}), (aggregator, locusCriteria) =>
                    (PipeToLocus(aggregator.Item1, locusCriteria, aggregator.Item2),
                        aggregator.Item2.Concat(new HashSet<Locus> {locusCriteria.l}).ToHashSet())
                );

            await foreach (var result in fullyMatched.Item1)
            {
                results[result.DonorId] = result;
            }

            return results?
                .Where(m => loci.All(l => matchFilteringService.FulfilsPerLocusMatchCriteria(m.Value, criteria, l)))
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m.Value, criteria))
                .ToDictionary();
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