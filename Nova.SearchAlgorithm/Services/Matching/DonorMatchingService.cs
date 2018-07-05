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
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchingDictionaryLookupService lookupService;

        public DonorMatchingService(IDatabaseDonorMatchingService databaseDonorMatchingService, IDonorInspectionRepository donorInspectionRepository,
            IMatchingDictionaryLookupService lookupService)
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
            this.donorInspectionRepository = donorInspectionRepository;
            this.lookupService = lookupService;
        }

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var threeLociMatches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, new List<Locus> {Locus.A, Locus.B, Locus.Drb1});

            var matchesWithDonorInfoPopulated = await Task.WhenAll(threeLociMatches.Select(PopulateDonorDataForMatch));

            var fiveLociMatches = matchesWithDonorInfoPopulated.Select(m =>
            {
                m.SetMatchDetailsForLocus(Locus.C, new LocusMatchDetails {MatchCount = 0});
                m.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 0});
                m.SetMatchDetailsForLocus(Locus.Dqb1, new LocusMatchDetails {MatchCount = 0});
                return m;
            });
            return fiveLociMatches;
        }

        private async Task<PotentialSearchResult> PopulateDonorDataForMatch(PotentialSearchResult potentialSearchResult)
        {
            // Augment each match with registry and other data from GetDonor(id)
            // Performance could be improved here, but at least it happens in parallel,
            // and only after filtering match results, not before.
            potentialSearchResult.Donor = await donorInspectionRepository.GetDonor(potentialSearchResult.DonorId);
            potentialSearchResult.Donor.MatchingHla = await potentialSearchResult.Donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n));
            return potentialSearchResult;
        }

        private LocusMatchDetails MatchDetails(AlleleLevelLocusMatchCriteria criteria, ExpandedHla hla1, ExpandedHla hla2)
        {
            var matchDetails = new LocusMatchDetails
            {
                MatchCount = 2, // Assume a match until we know otherwise
                IsLocusTyped = hla1 != null && hla2 != null,
            };

            if (criteria != null && hla1 != null && hla2 != null)
            {
                // We have typed search and donor hla to compare
                matchDetails.MatchCount = 0;

                // TODO:NOVA-1289 This sketch logic does not take into account some edge cases, like one patient position matching both donor positions,
                // which should count as only one match
                if (criteria.HlaNamesToMatchInPositionOne.Any(name =>
                    hla1.PGroups.Union(hla2.PGroups).Contains(name)))
                {
                    matchDetails.MatchCount += 1;
                }

                if (criteria.HlaNamesToMatchInPositionTwo.Any(name =>
                    hla1.PGroups.Union(hla2.PGroups).Contains(name)))
                {
                    matchDetails.MatchCount += 1;
                }
            }

            return matchDetails;
        }

        private Func<PotentialSearchResult, bool> FilterResultsByLocus(Locus locus, AlleleLevelMatchCriteria criteria)
        {
            return potentialSearchResult =>
            {
                var matchDetails = potentialSearchResult.MatchDetailsForLocus(locus);
                var locusCriteria = criteria.MatchCriteriaForLocus(locus);
                if (matchDetails == null)
                {
                    var donorHlaDataAtLocus = potentialSearchResult.Donor.MatchingHla.DataAtLocus(locus);
                    matchDetails = MatchDetails(locusCriteria, donorHlaDataAtLocus.Item1, donorHlaDataAtLocus.Item2);
                    potentialSearchResult.SetMatchDetailsForLocus(locus, matchDetails);
                }

                return matchDetails.MatchCount >= 2 - locusCriteria.MismatchCount;
            };
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