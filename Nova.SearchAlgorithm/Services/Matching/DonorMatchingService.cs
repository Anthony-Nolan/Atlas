using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDatabaseDonorMatchingService databaseDonorMatchingService;

        public DonorMatchingService(IDatabaseDonorMatchingService databaseDonorMatchingService)
        {
            this.databaseDonorMatchingService = databaseDonorMatchingService;
        }

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var threeLociMatches = await databaseDonorMatchingService.FindMatchesForLoci(criteria, new List<Locus> {Locus.A, Locus.B, Locus.Drb1});
            var fiveLociMatches = threeLociMatches.Select(m =>
            {
                m.SetMatchDetailsForLocus(Locus.C, new LocusMatchDetails {MatchCount = 0});
                m.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 0});
                m.SetMatchDetailsForLocus(Locus.Dqb1, new LocusMatchDetails {MatchCount = 0});
                return m;
            });
            return fiveLociMatches;
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
    }
}