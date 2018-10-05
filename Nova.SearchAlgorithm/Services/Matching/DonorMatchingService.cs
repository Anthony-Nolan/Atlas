using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;
        private readonly IDonorMatchCalculator donorMatchCalculator;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchFilteringService matchFilteringService;
        private readonly IMatchCriteriaAnalyser matchCriteriaAnalyser;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            IDonorMatchCalculator donorMatchCalculator,
            IDonorInspectionRepository donorInspectionRepository,
            IMatchFilteringService matchFilteringService, 
            IMatchCriteriaAnalyser matchCriteriaAnalyser)
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorMatchCalculator = donorMatchCalculator;
            this.donorInspectionRepository = donorInspectionRepository;
            this.matchFilteringService = matchFilteringService;
            this.matchCriteriaAnalyser = matchCriteriaAnalyser;
        }

        public async Task<IEnumerable<MatchResult>> GetMatches(AlleleLevelMatchCriteria criteria)
        {
            var lociToMatchInDatabase = matchCriteriaAnalyser.LociToMatchInDatabase(criteria).ToList();
            var lociToMatchInMemory = criteria.LociWithCriteriaSpecified().ToList().Except(lociToMatchInDatabase);

            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, lociToMatchInDatabase);

            var matchesWithPGroupsPopulated = (await PopulatePGroups(matches)).ToList();
            
            var matchesAtAllLoci = MatchInMemory(criteria, lociToMatchInMemory, matchesWithPGroupsPopulated);

            var filteredMatchesByMatchCriteria = matchesAtAllLoci
                .Where(m => matchFilteringService.FulfilsTotalMatchCriteria(m, criteria));
            
            var matchesWithDonorInfoPopulated = await PopulateDonorData(filteredMatchesByMatchCriteria);

            var filteredMatchesByDonorInformation = matchesWithDonorInfoPopulated
                .Where(m => matchFilteringService.FulfilsRegistryCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeCriteria(m, criteria))
                .Where(m => matchFilteringService.FulfilsSearchTypeSpecificCriteria(m, criteria))
                .ToList();
            
            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            filteredMatchesByDonorInformation.ForEach(m => m.MarkMatchingDataFullyPopulated());
            return filteredMatchesByDonorInformation;
        }

        /// <summary>
        /// Calculates match details for specified loci, and filters based on individual locus mismatch criteria
        /// </summary>
        /// <returns>A list of filtered search results, with the newly searched loci match information populated</returns>
        private IEnumerable<MatchResult> MatchInMemory(
            AlleleLevelMatchCriteria criteria,
            IEnumerable<Locus> lociToMatchInMemory,
            IEnumerable<MatchResult> matches)
        {
            foreach (var locus in lociToMatchInMemory)
            {
                var locusCriteria = criteria.MatchCriteriaForLocus(locus);
                foreach (var match in matches)
                {
                    var pGroupsAtLocus = match.DonorPGroups.DataAtLocus(locus);
                    var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(locusCriteria, pGroupsAtLocus);
                    match.SetMatchDetailsForLocus(locus, matchDetails);
                }

                matches = matches
                    .Where(m => matchFilteringService.FulfilsPerLocusMatchCriteria(m, criteria, locus))
                    .ToArray();
            }

            return matches;
        }

        private async Task<IEnumerable<MatchResult>> PopulatePGroups(IEnumerable<MatchResult> matchResults)
        {
            matchResults = matchResults.ToList();
            var pGroups = await donorInspectionRepository.GetPGroupsForDonors(matchResults.Select(r => r.DonorId));
            foreach (var donorIdWithPGroupNames in pGroups)
            {
                matchResults.Single(r => r.DonorId == donorIdWithPGroupNames.DonorId).DonorPGroups = donorIdWithPGroupNames.PGroupNames;
            }
            return matchResults;
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

        private async Task<MatchResult> PopulateDonorDataForMatch(MatchResult matchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            matchResult.Donor = await donorInspectionRepository.GetDonor(matchResult.DonorId);
            return matchResult;
        }
    }
}