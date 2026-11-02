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
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchingService
    {
        IAsyncEnumerable<MatchResult> GetMatches(MatchCriteria criteria, DateTimeOffset? cutOffDate);
    }

    public class MatchingService : IMatchingService
    {
        private readonly IDonorMatchingService donorMatchingService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IAtlasLogger searchLogger;
        private readonly MatchingConfigurationSettings matchingConfigurationSettings;

        public MatchingService(
            IDonorMatchingService donorMatchingService,
            IActiveRepositoryFactory transientRepositoryFactory,
            IMatchFilteringService matchFilteringService,
            IMatchingAlgorithmSearchLogger searchLogger,
            MatchingConfigurationSettings matchingConfigurationSettings)
        {
            this.donorMatchingService = donorMatchingService;
            donorInspectionRepository = transientRepositoryFactory.GetDonorInspectionRepository();
            this.matchFilteringService = matchFilteringService;
            this.searchLogger = searchLogger;
            this.matchingConfigurationSettings = matchingConfigurationSettings;
        }

        public async IAsyncEnumerable<MatchResult> GetMatches(MatchCriteria criteria, DateTimeOffset? cutOffDate)
        {
            using (searchLogger.RunTimed("Matching"))
            {
                var run = MatchingStageTimings.StartRun();
                foreach (var (locus, c) in criteria.AlleleLevelMatchCriteria.LocusCriteria.Map((l, c) => (l, c)).ToEnumerable().Where(x => x.Item2 != null))
                {
                    searchLogger.SendTrace(
                        $"Matching: Locus {locus} has ({c.PGroupsToMatchInPositionOne.Count()}, {c.PGroupsToMatchInPositionTwo.Count()}) P-Groups to match"
                    );
                }

                var initialMatches = PerformMatchingPhaseOne(criteria, cutOffDate);
                var matches = PerformMatchingPhaseTwo(criteria.AlleleLevelMatchCriteria, initialMatches);
                var matchCount = 0;
                await foreach (var match in matches)
                {
                    matchCount++;
                    var consumerStart = MatchingRunTimings.Now();
                    yield return match;
                    run?.AddConsumer(consumerStart);
                }

                searchLogger.SendTrace($"Matched {matchCount} donors");
                run?.Send(searchLogger, DescribeCriteria(criteria, cutOffDate));
            }
        }

        /// <summary>
        /// The first phase of matching performs the bulk of the work - it returns all donors that meet the matching criteria, at all specified loci.
        /// It must return a superset of the final matching donor set - i.e. no matching donors may exist and not be returned in this phase.
        /// </summary>
        private async IAsyncEnumerable<MatchResult> PerformMatchingPhaseOne(MatchCriteria criteria, DateTimeOffset? cutOffDate)
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
                MatchingStageTimings.Run?.SetPhase1Donors(count);
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
                var run = MatchingStageTimings.Run;
                await foreach (var resultBatch in matches.Chunk(matchingConfigurationSettings.MatchingBatchSize))
                {
                    var donorLoadStart = MatchingRunTimings.Now();
                    var matchesWithDonorInfoPopulated = await PopulateDonorData(resultBatch.ToDictionary(x => x.DonorId, x => x));
                    run?.AddDonorLoad(donorLoadStart, matchesWithDonorInfoPopulated.Count);

                    var filterStart = MatchingRunTimings.Now();
                    var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                        .Where(m => matchFilteringService.IsAvailableForSearch(m.Value))
                        .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m.Value, criteria))
                        .Where(m => matchFilteringService.FulfilsConfigurableMatchCountCriteria(m.Value, criteria))
                        .ToList();
                    // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
                    filteredMatchesByDonorInformation.ForEach(m => m.Value.MarkMatchingDataFullyPopulated());
                    run?.AddPhase2Filter(filterStart);
                    foreach (var result in filteredMatchesByDonorInformation)
                    {
                        count++;
                        yield return result.Value;
                    }
                }

                searchLogger.SendTrace($"Matching Phase 2: Found {count} donors.");
                run?.SetPhase2Donors(count);
            }
        }

        /// <summary>ATL-510 experiment: the shape of one sub-search, as properties of the "Matching timings" event. No patient HLA.</summary>
        private static Dictionary<string, string> DescribeCriteria(MatchCriteria criteria, DateTimeOffset? cutOffDate)
        {
            var alleleLevel = criteria.AlleleLevelMatchCriteria;
            var mismatches = alleleLevel.LocusCriteria
                .Map((locus, c) => c == null ? null : $"{locus}{c.MismatchCount}")
                .ToEnumerable()
                .Where(x => x != null);

            return new Dictionary<string, string>
            {
                ["Mismatches"] = string.Join(",", mismatches),
                ["DonorMismatchCount"] = alleleLevel.DonorMismatchCount.ToString(),
                ["SearchType"] = alleleLevel.SearchType.ToString(),
                ["HasCutOffDate"] = (cutOffDate != null).ToString(),
                ["RegistryCodeCount"] = (criteria.NonHlaFilteringCriteria?.RegistryCodes?.Count() ?? 0).ToString(),
            };
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