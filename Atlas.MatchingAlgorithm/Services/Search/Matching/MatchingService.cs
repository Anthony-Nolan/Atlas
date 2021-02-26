using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Settings;
using Dasync.Collections;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchingService
    {
        IAsyncEnumerable<MatchResult> GetMatches(AlleleLevelMatchCriteria criteria, DateTimeOffset? cutOffDate);
    }

    public class MatchingService : IMatchingService
    {
        private readonly IDonorMatchingService donorMatchingService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly ILogger searchLogger;
        private readonly MatchingConfigurationSettings matchingConfigurationSettings;

        public MatchingService(
            IDonorMatchingService donorMatchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory transientRepositoryFactory,
            IMatchFilteringService matchFilteringService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            MatchingConfigurationSettings matchingConfigurationSettings)
        {
            this.donorMatchingService = donorMatchingService;
            donorInspectionRepository = transientRepositoryFactory.GetDonorInspectionRepository();
            this.matchFilteringService = matchFilteringService;
            this.searchLogger = searchLogger;
            this.matchingConfigurationSettings = matchingConfigurationSettings;
        }

        public async IAsyncEnumerable<MatchResult> GetMatches(AlleleLevelMatchCriteria criteria, DateTimeOffset? cutOffDate)
        {
            using (searchLogger.RunTimed("Matching"))
            {
                foreach (var (locus, c) in criteria.LocusCriteria.Map((l, c) => (l, c)).ToEnumerable().Where(x => x.Item2 != null))
                {
                    searchLogger.SendTrace(
                        $"Matching: Locus {locus} has ({c.PGroupsToMatchInPositionOne.Count()}, {c.PGroupsToMatchInPositionTwo.Count()}) P-Groups to match"
                    );
                }

                var initialMatches = PerformMatchingPhaseOne(criteria, cutOffDate);
                var matches = PerformMatchingPhaseTwo(criteria, initialMatches);
                var matchCount = 0;
                await foreach (var match in matches)
                {
                    matchCount++;
                    yield return match;
                }

                searchLogger.SendTrace($"Matched {matchCount} donors");
            }
        }

        /// <summary>
        /// The first phase of matching performs the bulk of the work - it returns all donors that meet the matching criteria, at all specified loci.
        /// It must return a superset of the final matching donor set - i.e. no matching donors may exist and not be returned in this phase.
        /// </summary>
        private async IAsyncEnumerable<MatchResult> PerformMatchingPhaseOne(AlleleLevelMatchCriteria criteria, DateTimeOffset? cutOffDate)
        {
            using (searchLogger.RunTimed("Matching timing: Phase 1 complete"))
            {
                var matches = await donorMatchingService.FindMatchingDonors(criteria, cutOffDate);
                var count = 0;
                await foreach (var match in matches)
                {
                    count++;
                    yield return match;
                }

                searchLogger.SendTrace($"Matching Phase 1: Found {count} donors.");
            }
        }

        /// <summary>
        /// The second phase of matching does not need to query the p-group matching tables.
        /// It will assess the matches from all individual loci against the remaining search criteria.
        ///
        /// Any filtering performed on non-hla donor info is performed here, as well as any search-type specific criteria.  
        /// </summary>
        private async IAsyncEnumerable<MatchResult> PerformMatchingPhaseTwo(AlleleLevelMatchCriteria criteria, IAsyncEnumerable<MatchResult> matches)
        {
            using (searchLogger.RunTimed("Matching timing: Phase 2 complete"))
            {
                var count = 0;
                await foreach (var resultBatch in matches.Batch(matchingConfigurationSettings.MatchingBatchSize))
                {
                    var matchesWithDonorInfoPopulated = await PopulateDonorData(resultBatch.ToDictionary(x => x.DonorId, x => x));
                    var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                        .Where(m => matchFilteringService.IsAvailableForSearch(m.Value))
                        .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m.Value, criteria))
                        .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m.Value, criteria))
                        .ToList();
                    // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
                    filteredMatchesByDonorInformation.ForEach(m => m.Value.MarkMatchingDataFullyPopulated());
                    foreach (var result in filteredMatchesByDonorInformation)
                    {
                        count++;
                        yield return result.Value;
                    }
                }

                searchLogger.SendTrace($"Matching Phase 2: Found {count} donors.");
            }
        }

        private async Task<IDictionary<int, MatchResult>> PopulateDonorData(Dictionary<int, MatchResult> filteredMatchesByMatchCriteria)
        {
            var donors = await donorInspectionRepository.GetDonors(filteredMatchesByMatchCriteria.Keys);
            foreach (var (donorId, matchResult) in filteredMatchesByMatchCriteria)
            {
                matchResult.DonorInfo = donors[donorId];
            }

            return filteredMatchesByMatchCriteria;
        }
    }
}