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
        private readonly IMatchingDictionaryLookupService lookupService;

        public DonorMatchingService(
            IDatabaseDonorMatchingService databaseDonorMatchingService,
            IDonorMatchCalculator donorMatchCalculator,
            IDonorInspectionRepository donorInspectionRepository,
            IMatchingDictionaryLookupService lookupService
        )
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorMatchCalculator = donorMatchCalculator;
            this.donorInspectionRepository = donorInspectionRepository;
            this.lookupService = lookupService;
        }

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var allLoci = new List<Locus> {Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1};
            var lociToSearch = criteria.LociWithCriteriaSpecified().ToList();
            
            // TODO: NOVA-1395: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
            var lociToMatchInDatabase = new List<Locus> {Locus.A, Locus.B, Locus.Drb1}.Intersect(lociToSearch);
            var lociToMatchInMemory = new List<Locus> {Locus.C, Locus.Dqb1}.Intersect(lociToSearch);

            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, lociToMatchInDatabase);

            var matchesWithDonorInfoPopulated = await Task.WhenAll(matches.Select(PopulateDonorDataForMatch));

            foreach (var locus in lociToMatchInMemory)
            {
                var locusCriteria = criteria.MatchCriteriaForLocus(locus);
                foreach (var match in matchesWithDonorInfoPopulated)
                {
                    var donorDataAtLocus = match.Donor.MatchingHla.DataAtLocus(locus);
                    var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(locusCriteria, donorDataAtLocus);
                    match.SetMatchDetailsForLocus(locus, matchDetails);
                }

                matchesWithDonorInfoPopulated = matchesWithDonorInfoPopulated
                    // TODO: Commonise filtering logic, used here and in database matching layer. Wants to be done after each locus to reduce number of results for next calculation
                    .Where(m => m.MatchDetailsForLocus(locus).MatchCount >= 2 - locusCriteria.MismatchCount)
                    .ToArray();
            }

            // TODO: Figure out if this is the best way to handle loci with no patient data specified
            foreach (var locus in allLoci.Except(lociToSearch))
            {
                matchesWithDonorInfoPopulated.ToList().ForEach(m => m.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 0}));
            }

            // TODO: Commonise with total score in databse matching, use number of populated loci? 
            return matchesWithDonorInfoPopulated.Where(m => m.TotalMatchCount >= 6 - criteria.DonorMismatchCount);
        }

        // TODO: NOVA-1289: Lookup PGroups from matches table, rather than fetching donor and performing lookup again - with an index on DonorId it should be faster. (We will still need to fetch donor type + registry later)
        private async Task<PotentialSearchResult> PopulateDonorDataForMatch(PotentialSearchResult potentialSearchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            potentialSearchResult.Donor = await donorInspectionRepository.GetDonor(potentialSearchResult.DonorId);
            potentialSearchResult.Donor.MatchingHla = await potentialSearchResult.Donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n));
            return potentialSearchResult;
        }

        private async Task<ExpandedHla> Lookup(Locus locus, string hla)
        {
            if (locus.Equals(Locus.Dpb1))
            {
                // TODO:NOVA-1300 figure out how best to lookup matches for Dpb1
                return null;
            }

            return hla == null
                ? null
                : (await lookupService.GetMatchingHla(locus.ToMatchLocus(), hla)).ToExpandedHla(hla);
        }
    }
}