using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchingService
    {
        Task<IList<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria);
    }

    public class MatchingService : IMatchingService
    {
        private readonly IDonorMatchingService donorMatchingService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly ILogger searchLogger;

        public MatchingService(
            IDonorMatchingService donorMatchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory transientRepositoryFactory,
            IMatchFilteringService matchFilteringService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger)
        {
            this.donorMatchingService = donorMatchingService;
            donorInspectionRepository = transientRepositoryFactory.GetDonorInspectionRepository();
            this.matchFilteringService = matchFilteringService;
            this.searchLogger = searchLogger;
        }

        public async Task<IList<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria)
        {
            var initialMatches = await PerformMatchingPhaseOne(criteria);
            var matchesAtAllLoci = initialMatches;
            return (await PerformMatchingPhaseTwo(criteria, matchesAtAllLoci)).Values.ToList();
        }

        /// <summary>
        /// The first phase of matching performs the bulk of the work - it returns all donors that meet the matching criteria, at all specified loci.
        /// It must return a superset of the final matching donor set - i.e. no matching donors may exist and not be returned in this phase.
        /// </summary>
        private async Task<IDictionary<int, MatchResult>> PerformMatchingPhaseOne(AlleleLevelMatchCriteria criteria)
        {
            using (searchLogger.RunTimed("Matching timing: Phase 1 complete"))
            {
                var matches = await donorMatchingService.FindMatchingDonors(criteria);
                searchLogger.SendTrace($"Matching Phase 1: Found {matches.Count} donors.");
                return matches;
            }
        }

        /// <summary>
        /// The second phase of matching does not need to query the p-group matching tables.
        /// It will assess the matches from all individual loci against the remaining search criteria.
        ///
        /// Any filtering performed on non-hla donor info is performed here, as well as any search-type specific criteria.  
        /// </summary>
        private async Task<IDictionary<int, MatchResult>> PerformMatchingPhaseTwo(
            AlleleLevelMatchCriteria criteria,
            IDictionary<int, MatchResult> matches
        )
        {
            using (searchLogger.RunTimed("Matching timing: Phase 2 complete"))
            {
                var filteredMatchesByMatchCriteria = matches
                    .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m.Value, criteria))
                    .ToDictionary(m => m.Key, m => m.Value);

                var matchesWithDonorInfoPopulated = await PopulateDonorData(filteredMatchesByMatchCriteria);

                var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                    .Where(m => matchFilteringService.IsAvailableForSearch(m.Value))
                    .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m.Value, criteria))
                    .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m.Value, criteria))
                    .ToList();

                // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
                filteredMatchesByDonorInformation.ForEach(m => m.Value.MarkMatchingDataFullyPopulated());

                searchLogger.SendTrace($"Matching Phase 2: Found {filteredMatchesByDonorInformation.Count} donors.");
                return filteredMatchesByDonorInformation.ToDictionary();
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