using System;
using System.Collections.Generic;
using System.Linq;
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

            var matchesWithDonorInfoPopulated = await Task.WhenAll(matches.Select(PopulateDonorDataForMatch));

            matchesWithDonorInfoPopulated = MatchInMemory(criteria, lociToMatchInMemory, matchesWithDonorInfoPopulated);

            // TODO: Figure out if this is the best way to handle loci with no patient data specified
            foreach (var locus in allLoci.Except(lociToSearch))
            {
                matchesWithDonorInfoPopulated.ToList().ForEach(m => m.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 0}));
            }

            // TODO: Commonise with total score in databse matching, use number of populated loci? 
            return matchesWithDonorInfoPopulated.Where(m => m.TotalMatchCount >= (lociToSearch.Count * 2) - criteria.DonorMismatchCount);
        }

        /// <summary>
        /// Calculates match details for specified loci, and filters based on individual locus mismatch criteria
        /// </summary>
        /// <returns>A list of filtered search results, with the newly searched loci match information populated</returns>
        private PotentialSearchResult[] MatchInMemory(
            AlleleLevelMatchCriteria criteria,
            IEnumerable<Locus> lociToMatchInMemory,
            PotentialSearchResult[] matches)
        {
            foreach (var locus in lociToMatchInMemory)
            {
                var locusCriteria = criteria.MatchCriteriaForLocus(locus);
                foreach (var match in matches)
                {
                    var donorDataAtLocus = match.Donor.MatchingHla.DataAtLocus(locus);
                    var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(locusCriteria, donorDataAtLocus);
                    match.SetMatchDetailsForLocus(locus, matchDetails);
                }

                matches = matches
                    // TODO: Commonise filtering logic, used here and in database matching layer. Wants to be done after each locus to reduce number of results for next calculation
                    .Where(m => m.MatchDetailsForLocus(locus).MatchCount >= 2 - locusCriteria.MismatchCount)
                    .ToArray();
            }

            return matches;
        }

        // TODO: NOVA-1289: Lookup PGroups from matches table, rather than fetching donor and performing lookup again - with an index on DonorId it should be faster. (We will still need to fetch donor type + registry later)
        private async Task<PotentialSearchResult> PopulateDonorDataForMatch(PotentialSearchResult potentialSearchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            potentialSearchResult.Donor = await donorInspectionRepository.GetDonor(potentialSearchResult.DonorId);

            // Note that this will only populate PGroups in the expanded HLA object returned. This should be enough for matching, but is not ideal
            // TODO: Just fetch p-groups and filter on p-groups, only expand donro once all filtering finished
            var expandedHla = await donorInspectionRepository.GetExpandedHlaForDonor(potentialSearchResult.DonorId);
            
            potentialSearchResult.Donor.MatchingHla = expandedHla;
            return potentialSearchResult;
        }
    }
}