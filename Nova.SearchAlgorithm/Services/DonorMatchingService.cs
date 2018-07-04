using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria);
    }
    
    public class DonorMatchingService: IDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;

        public DonorMatchingService(IDonorSearchRepository donorSearchRepository, IDonorInspectionRepository donorInspectionRepository)
        {
            this.donorSearchRepository = donorSearchRepository;
            this.donorInspectionRepository = donorInspectionRepository;
        }
        
        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var threeLociMatches = await ThreeLociSearch(criteria);

            var fiveLociMatches = threeLociMatches;
//                .Select(AddMatchCounts(criteria))
//                .Where(FilterByMismatchCriteria(criteria));

            return fiveLociMatches;
        }

        private async Task<IEnumerable<PotentialSearchResult>> ThreeLociSearch(AlleleLevelMatchCriteria matchRequest)
        {
            var results = await Task.WhenAll(
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.A, matchRequest.LocusMismatchA),
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.B, matchRequest.LocusMismatchB),
                FindMatchesAtLocus(matchRequest.SearchType, matchRequest.RegistriesToSearch, Locus.Drb1, matchRequest.LocusMismatchDRB1));

            var matchesAtA = results[0];
            var matchesAtB = results[1];
            var matchesAtDrb1 = results[2];

            var matches = await Task.WhenAll(matchesAtA.Union(matchesAtB).Union(matchesAtDrb1)
                .GroupBy(m => m.Key)
                .Select(g => new PotentialSearchResult
                {
                    Donor = g.First().Value.Donor ?? new DonorResult() { DonorId = g.Key },
                    TotalMatchCount = g.Sum(m => m.Value.Match.MatchCount),
                    MatchDetailsAtLocusA = matchesAtA.ContainsKey(g.Key) ? matchesAtA[g.Key].Match : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusB = matchesAtB.ContainsKey(g.Key) ? matchesAtB[g.Key].Match : new LocusMatchDetails { MatchCount = 0 },
                    MatchDetailsAtLocusDrb1 = matchesAtDrb1.ContainsKey(g.Key) ? matchesAtDrb1[g.Key].Match : new LocusMatchDetails { MatchCount = 0 },
                })
                .Where(m => m.TotalMatchCount >= 6 - matchRequest.DonorMismatchCount)
                .Where(m => m.MatchDetailsAtLocusA.MatchCount >= 2 - matchRequest.LocusMismatchA.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusB.MatchCount >= 2 - matchRequest.LocusMismatchB.MismatchCount)
                .Where(m => m.MatchDetailsAtLocusDrb1.MatchCount >= 2 - matchRequest.LocusMismatchDRB1.MismatchCount)
                .Select(async m =>
                {
                    // Augment each match with registry and other data from GetDonor(id)
                    // Performance could be improved here, but at least it happens in parallel,
                    // and only after filtering match results, not before.
                    // In the cosmos case this is already populated, so we don't bother if the donor hla isn't null.
                    m.Donor = m.Donor.MatchingHla != null ? m.Donor : await donorInspectionRepository.GetDonor(m.Donor.DonorId);
                    return m;
                })
            );
            
            return matches;
        }
        
        private async Task<IDictionary<int, DonorAndMatch>> FindMatchesAtLocus(DonorType searchType, IEnumerable<RegistryCode> registriesToSearch, Locus locus, AlleleLevelLocusMatchCriteria criteria)
        {
            LocusSearchCriteria repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                HlaNamesToMatchInPositionOne = criteria.HlaNamesToMatchInPositionOne,
                HlaNamesToMatchInPositionTwo = criteria.HlaNamesToMatchInPositionTwo,
            };

            var matches = (await donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, DonorAndMatchFromGroup);

            return matches;
        }

        private DonorAndMatch DonorAndMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group)
        {
            return new DonorAndMatch
            {
                Donor = group.First()?.Donor,
                Match = new LocusMatchDetails
                {
                    MatchCount = DirectMatch(group) || CrossMatch(group) ? 2 : 1
                }
            };
        }

        private bool DirectMatch(IEnumerable<PotentialHlaMatchRelation> matches)
        {
            return matches.Any(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.One))
                   && matches.Any(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.Two));
        }

        private bool CrossMatch(IEnumerable<PotentialHlaMatchRelation> matches)
        {
            return matches.Any(m => m.SearchTypePosition == TypePositions.One && m.MatchingTypePositions.HasFlag(TypePositions.Two))
                   && matches.Any(m => m.SearchTypePosition == TypePositions.Two && m.MatchingTypePositions.HasFlag(TypePositions.One));
        }
        
        private Func<PotentialSearchResult, PotentialSearchResult> AddMatchCounts(AlleleLevelMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) add match counts based on C and DBQR
            // TODO:NOVA-1289 implement typed loci booleans and counts
            return potentialSearchResult =>
            {
                var donorHla = potentialSearchResult.Donor.MatchingHla;

                potentialSearchResult.MatchDetailsAtLocusC =
                    MatchDetails(criteria.LocusMismatchC, donorHla?.C_1, donorHla?.C_2);

                potentialSearchResult.MatchDetailsAtLocusDqb1 =
                    MatchDetails(criteria.LocusMismatchC, donorHla?.DQB1_1, donorHla?.DQB1_2);

                potentialSearchResult.TotalMatchCount += potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount;
                potentialSearchResult.TotalMatchCount += potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount;
                
                return potentialSearchResult;
            };
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

        private Func<PotentialSearchResult, bool> FilterByMismatchCriteria(AlleleLevelMatchCriteria criteria)
        {
            // TODO:NOVA-1289 (create tests and) filter based on total match count and all 5 loci match counts
            return potentialSearchResult =>
            {
                if (potentialSearchResult.MatchDetailsAtLocusC != null &&
                    criteria.LocusMismatchC != null &&
                    potentialSearchResult.MatchDetailsAtLocusC.MatchCount < criteria.LocusMismatchC.MismatchCount)
                {
                    return false;
                }

                if (potentialSearchResult.MatchDetailsAtLocusDqb1 != null &&
                    criteria.LocusMismatchDQB1 != null &&
                    potentialSearchResult.MatchDetailsAtLocusDqb1.MatchCount < criteria.LocusMismatchDQB1.MismatchCount)
                {
                    return false;
                }

                // TODO:NOVA-1289 take into account cord or adult search differences
                if (potentialSearchResult.TotalMatchCount < criteria.DonorMismatchCount)
                {
                    return false;
                }

                return true;
            };
        }

        private class DonorAndMatch
        {
            public LocusMatchDetails Match { get; set; }
            public DonorResult Donor { get; set; }
        }
    }
}