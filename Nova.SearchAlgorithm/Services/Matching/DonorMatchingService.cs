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
            var matches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, new List<Locus> {Locus.B, Locus.Drb1});

            var matchesWithDonorInfoPopulated = await Task.WhenAll(matches.Select(PopulateDonorDataForMatch));

            var lociToMatchInMemory = new List<Locus> {Locus.A};
            foreach (var locus in lociToMatchInMemory)
            {
                foreach (var match in matchesWithDonorInfoPopulated)
                {
                    var matchDetails = donorMatchCalculator
                        .CalculateMatchDetailsForDonorHla(criteria.MatchCriteriaForLocus(locus), match.Donor.MatchingHla.DataAtLocus(locus));
                    match.SetMatchDetailsForLocus(locus, matchDetails);
                }
                matchesWithDonorInfoPopulated = matchesWithDonorInfoPopulated
                        // TODO: Commonise filtering logic, used here and in database matching layer. Wants to be done after each locus to reduce number of results for next calculation
                    .Where(m => lociToMatchInMemory.All(l => m.MatchDetailsForLocus(l).MatchCount >= 2 - criteria.MatchCriteriaForLocus(l).MismatchCount))
                    .ToArray();
            }

            var fiveLociMatches = matchesWithDonorInfoPopulated.Select(m =>
            {
                m.SetMatchDetailsForLocus(Locus.C, new LocusMatchDetails {MatchCount = 0});
                m.SetMatchDetailsForLocus(Locus.Dqb1, new LocusMatchDetails {MatchCount = 0});
                return m;
            });
            
            // TODO: Commonise with total score in databse matching, use number of populated loci? 
            return fiveLociMatches.Where(m => m.TotalMatchCount >= 6 - criteria.DonorMismatchCount);
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