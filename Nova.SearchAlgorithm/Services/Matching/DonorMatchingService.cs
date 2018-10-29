using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Repositories;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.Matching
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
            IDonorInspectionRepository donorInspectionRepository,
            IMatchFilteringService matchFilteringService,
            IMatchCriteriaAnalyser matchCriteriaAnalyser,
            ILogger logger
        )
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorInspectionRepository = donorInspectionRepository;
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
            return await PerformMatchingPhaseThree(criteria, matchesAtAllLoci);
        }

        /// <summary>
        /// The first phase of matching must perform a full scan of the MatchingHla tables for the specified loci
        /// It must return a superset of the final matching donor set - i.e. no matching donors may exist and not be returned in this phase
        /// </summary>
        private async Task<IEnumerable<MatchResult>> PerformMatchingPhaseOne(AlleleLevelMatchCriteria criteria, IList<Locus> loci)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matches = (await databaseDonorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            logger.SendTrace("Matching timing: Phase 1 complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"Donors", matches.Count.ToString()},
                {"Loci", string.Join(",", loci.Select(l => l.ToString()))}
            });

            return matches;
        }

        /// <summary>
        /// The second phase of matching needs only consider the donors matched by phase 1, and filter out mismatches at the remaining loci
        /// </summary>
        private async Task<IEnumerable<MatchResult>> PerformMatchingPhaseTwo(
            AlleleLevelMatchCriteria criteria,
            IList<Locus> loci,
            IEnumerable<MatchResult> initialMatches
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var matchesAtAllLoci =
                (await databaseDonorMatchingService.FindMatchesForLociFromDonorSelection(criteria, loci, initialMatches))
                .ToList();
            
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
        private async Task<IEnumerable<MatchResult>> PerformMatchingPhaseThree(
            AlleleLevelMatchCriteria criteria,
            IEnumerable<MatchResult> matches
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var filteredMatchesByMatchCriteria = matches.Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria));

            var matchesWithDonorInfoPopulated = await PopulateDonorData(filteredMatchesByMatchCriteria);

            var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                .Where(m => matchFilteringService.FulfilsRegistryCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m, criteria))
                .ToList();

            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            filteredMatchesByDonorInformation.ForEach(m => m.MarkMatchingDataFullyPopulated());
            
            logger.SendTrace("Matching timing: Phase 3 complete", LogLevel.Info, new Dictionary<string, string>
            {
                {"Milliseconds", stopwatch.ElapsedMilliseconds.ToString()},
                {"Donors", filteredMatchesByDonorInformation.Count.ToString()},
            });
            return filteredMatchesByDonorInformation;
        }

        private async Task<IEnumerable<MatchResult>> PopulateDonorData(IEnumerable<MatchResult> filteredMatchesByMatchCriteria)
        {
            filteredMatchesByMatchCriteria = filteredMatchesByMatchCriteria.ToList();
            var donorIds = filteredMatchesByMatchCriteria.Select(m => m.DonorId);
            var donors = (await donorInspectionRepository.GetDonors(donorIds)).ToList();
            foreach (var match in filteredMatchesByMatchCriteria)
            {
                match.Donor = donors.Single(d => d.DonorId == match.DonorId);
            }

            return filteredMatchesByMatchCriteria;
        }
    }
}