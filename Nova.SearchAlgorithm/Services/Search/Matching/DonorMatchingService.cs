using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.SearchResults;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;

namespace Nova.SearchAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IMatchCriteriaAnalyser matchCriteriaAnalyser;
        private readonly ILogger logger;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IActiveRepositoryFactory transientRepositoryFactory,
            IMatchFilteringService matchFilteringService,
            IMatchCriteriaAnalyser matchCriteriaAnalyser,
            ILogger logger)
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            donorInspectionRepository = transientRepositoryFactory.GetDonorInspectionRepository();
            this.matchFilteringService = matchFilteringService;
            this.matchCriteriaAnalyser = matchCriteriaAnalyser;
            this.logger = logger;
        }

        public async Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria)
        {
            var lociToMatchFirst = matchCriteriaAnalyser.LociToMatchFirst(criteria).ToList();
            var lociToMatchSecond = criteria.LociWithCriteriaSpecified().Except(lociToMatchFirst).ToList();

            var initialMatches = await PerformMatchingPhaseOne(criteria, lociToMatchFirst);
            var matchesAtAllLoci = await PerformMatchingPhaseTwo(criteria, lociToMatchSecond, initialMatches);
            return (await PerformMatchingPhaseThree(criteria, matchesAtAllLoci)).Values;
        }

        /// <summary>
        /// The first phase of matching must perform a full scan of the MatchingHla tables for the specified loci.
        /// It must return a superset of the final matching donor set - i.e. no matching donors may exist and not be returned in this phase.
        /// </summary>
        private async Task<IDictionary<int, MatchResult>> PerformMatchingPhaseOne(AlleleLevelMatchCriteria criteria, ICollection<Locus> loci)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, loci);

            logger.SendTrace("Matching timing: Phase 1 complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"Donors", matches.Count.ToString()},
                {"Loci", string.Join(",", loci.Select(l => l.ToString()))}
            });

            return matches;
        }

        /// <summary>
        /// The second phase of matching needs only consider the donors matched by phase 1, and filter out mismatches at the remaining loci.
        /// </summary>
        private async Task<IDictionary<int, MatchResult>> PerformMatchingPhaseTwo(
            AlleleLevelMatchCriteria criteria,
            ICollection<Locus> loci,
            IDictionary<int, MatchResult> initialMatches
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matchesAtAllLoci = await databaseDonorMatchingService.FindMatchesForLociFromDonorSelection(criteria, loci, initialMatches);

            logger.SendTrace("Matching timing: Phase 2 complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"Donors", matchesAtAllLoci.Count.ToString()},
                {"Loci", string.Join(",", loci.Select(l => l.ToString()))}
            });
            return matchesAtAllLoci;
        }

        /// <summary>
        /// The third phase of matching does not need to query the p-group matching tables.
        /// It will assess the matches from all individual loci against the remaining search criteria
        /// </summary>
        private async Task<IDictionary<int, MatchResult>> PerformMatchingPhaseThree(
            AlleleLevelMatchCriteria criteria,
            IDictionary<int, MatchResult> matches
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var filteredMatchesByMatchCriteria = matches
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m.Value, criteria))
                .ToDictionary(m => m.Key, m => m.Value);

            var matchesWithDonorInfoPopulated = await PopulateDonorData(filteredMatchesByMatchCriteria);

            var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                .Where(m => matchFilteringService.IsAvailableForSearch(m.Value))
                .Where(m => matchFilteringService.FulfilsRegistryCriteria(m.Value, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m.Value, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m.Value, criteria))
                .ToList();

            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            filteredMatchesByDonorInformation.ForEach(m => m.Value.MarkMatchingDataFullyPopulated());

            logger.SendTrace("Matching timing: Phase 3 complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"Donors", filteredMatchesByDonorInformation.Count.ToString()},
            });
            return filteredMatchesByDonorInformation.ToDictionary(m => m.Key, m => m.Value);
        }

        private async Task<IDictionary<int, MatchResult>> PopulateDonorData(Dictionary<int, MatchResult> filteredMatchesByMatchCriteria)
        {
            var donors = await donorInspectionRepository.GetDonors(filteredMatchesByMatchCriteria.Keys);
            foreach (var (donorId, matchResult) in filteredMatchesByMatchCriteria)
            {
                matchResult.Donor = donors[donorId];
            }

            return filteredMatchesByMatchCriteria;
        }
    }
}