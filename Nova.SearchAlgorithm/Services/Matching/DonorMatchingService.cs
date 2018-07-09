using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;
        private readonly IDonorMatchCalculator donorMatchCalculator;
        private readonly IDonorInspectionRepository donorInspectionRepository;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            IDonorMatchCalculator donorMatchCalculator,
            IDonorInspectionRepository donorInspectionRepository
        )
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorMatchCalculator = donorMatchCalculator;
            this.donorInspectionRepository = donorInspectionRepository;
        }

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var allLoci = new List<Locus> {Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1};
            var lociToSearch = criteria.LociWithCriteriaSpecified().ToList();

            // TODO: NOVA-1395: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
            // Need to consider the 2 mismatch case for a locus - the database search will assume at least one match, so is not suitable for two mismatch loci
            var lociToMatchInDatabase = new List<Locus> {Locus.B, Locus.Drb1}.Intersect(lociToSearch).ToList();
            var lociToMatchInMemory = new List<Locus> {Locus.A, Locus.C, Locus.Dqb1}.Intersect(lociToSearch);

            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, lociToMatchInDatabase);

            var matchesWithPGroupsPopulated = (await Task.WhenAll(matches.Select(PopulatePGroupsForMatch))).AsEnumerable();
            
            matchesWithPGroupsPopulated = MatchInMemory(criteria, lociToMatchInMemory, matchesWithPGroupsPopulated);

            // TODO: Commonise with total score in databse matching, use number of populated loci? 
            matchesWithPGroupsPopulated = matchesWithPGroupsPopulated.Where(m => m.TotalMatchCount >= (lociToSearch.Count * 2) - criteria.DonorMismatchCount);
            var matchesWithDonorInfoPopulated = await Task.WhenAll(matchesWithPGroupsPopulated.Select(PopulateDonorDataForMatch));
            
            // Once finished populating match data, mark data as populated (so that null locus match data can be accessed for mapping to the api model)
            matchesWithDonorInfoPopulated.ToList().ForEach(m => m.MarkMatchingDataFullyPopulated());
            return matchesWithDonorInfoPopulated;
        }

        /// <summary>
        /// Calculates match details for specified loci, and filters based on individual locus mismatch criteria
        /// </summary>
        /// <returns>A list of filtered search results, with the newly searched loci match information populated</returns>
        private IEnumerable<PotentialSearchResult> MatchInMemory(
            AlleleLevelMatchCriteria criteria,
            IEnumerable<Locus> lociToMatchInMemory,
            IEnumerable<PotentialSearchResult> matches)
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
                    // TODO: Commonise filtering logic, used here and in database matching layer. Wants to be done after each locus to reduce number of results for next calculation
                    .Where(m => m.MatchDetailsForLocus(locus).MatchCount >= 2 - locusCriteria.MismatchCount)
                    .ToArray();
            }

            return matches;
        }

        private async Task<PotentialSearchResult> PopulatePGroupsForMatch(PotentialSearchResult potentialSearchResult)
        {
            var pGroups = await donorInspectionRepository.GetPGroupsForDonor(potentialSearchResult.DonorId);
            potentialSearchResult.DonorPGroups = pGroups;
            return potentialSearchResult;
        }

        private async Task<PotentialSearchResult> PopulateDonorDataForMatch(PotentialSearchResult potentialSearchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            potentialSearchResult.Donor = await donorInspectionRepository.GetDonor(potentialSearchResult.DonorId);
            return potentialSearchResult;
        }
    }
}