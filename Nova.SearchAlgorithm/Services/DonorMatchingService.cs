using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Services
{
    public interface IDonorMatchingService
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria);
    }

    public class DonorMatchingService : IDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;
        private readonly IMatchingDictionaryLookupService lookupService;

        public DonorMatchingService(IDonorSearchRepository donorSearchRepository, IDonorInspectionRepository donorInspectionRepository, IMatchingDictionaryLookupService lookupService)
        {
            this.donorSearchRepository = donorSearchRepository;
            this.donorInspectionRepository = donorInspectionRepository;
            this.lookupService = lookupService;
        }

        public async Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var threeLociMatches = await SearchDatabaseForLoci(criteria, new List<Locus> {Locus.A, Locus.B, Locus.Drb1});
            return threeLociMatches;
        }

        private async Task<IEnumerable<PotentialSearchResult>> SearchDatabaseForLoci(AlleleLevelMatchCriteria criteria, IReadOnlyList<Locus> loci)
        {
            var results = await Task.WhenAll(loci.Select(l => FindMatchesAtLocus(criteria.SearchType, criteria.RegistriesToSearch, l, criteria.MatchCriteriaForLocus(l))));

            var matches = results
                .SelectMany(r => r)
                .GroupBy(m => m.Key)
                .Select(matchesForDonor =>
                {
                    var donorId = matchesForDonor.Key;
                    var result = new PotentialSearchResult
                    {
                        Donor = matchesForDonor.First().Value.Donor ?? new DonorResult {DonorId = donorId},
                    };
                    foreach (var locus in loci)
                    {
                        var matchesAtLocus = matchesForDonor.FirstOrDefault(m => m.Value.Locus == locus);
                        var locusMatchDetails = matchesAtLocus.Value != null ? matchesAtLocus.Value.Match : new LocusMatchDetails { MatchCount = 0 };
                        result.SetMatchDetailsForLocus(locus, locusMatchDetails);
                    }
                    return result;
                })
                .Where(m => m.TotalMatchCount >= 6 - criteria.DonorMismatchCount)
                .Where(m => loci.All(l => m.MatchDetailsForLocus(l).MatchCount >= 2 - criteria.MatchCriteriaForLocus(l).MismatchCount));
            
            var matchesWithDonorInfoExpanded = await Task.WhenAll(matches.Select(async m =>
            {
                // Augment each match with registry and other data from GetDonor(id)
                // Performance could be improved here, but at least it happens in parallel,
                // and only after filtering match results, not before.
                // In the cosmos case this is already populated, so we don't bother if the donor hla isn't null.
                m.Donor = m.Donor.MatchingHla != null ? m.Donor : await donorInspectionRepository.GetDonor(m.Donor.DonorId);
                m.Donor.MatchingHla = await m.Donor.HlaNames.WhenAllPositions((l, p, n) => Lookup(l, n));
                return m;
            }));

            return matchesWithDonorInfoExpanded;
        }

        private async Task<IDictionary<int, DonorAndMatchForLocus>> FindMatchesAtLocus(DonorType searchType, IEnumerable<RegistryCode> registriesToSearch, Locus locus, AlleleLevelLocusMatchCriteria criteria)
        {
            var repoCriteria = new LocusSearchCriteria
            {
                SearchType = searchType,
                Registries = registriesToSearch,
                HlaNamesToMatchInPositionOne = criteria.HlaNamesToMatchInPositionOne,
                HlaNamesToMatchInPositionTwo = criteria.HlaNamesToMatchInPositionTwo,
            };

            var matches = (await donorSearchRepository.GetDonorMatchesAtLocus(locus, repoCriteria))
                .GroupBy(m => m.DonorId)
                .ToDictionary(g => g.Key, g => DonorAndMatchFromGroup(g, locus));

            return matches;
        }

        private DonorAndMatchForLocus DonorAndMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group, Locus locus)
        {
            return new DonorAndMatchForLocus
            {
                Donor = group.First()?.Donor,
                Match = new LocusMatchDetails
                {
                    MatchCount = DirectMatch(group) || CrossMatch(group) ? 2 : 1
                },
                Locus = locus
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

        private class DonorAndMatchForLocus
        {
            public LocusMatchDetails Match { get; set; }
            public DonorResult Donor { get; set; }
            public Locus Locus { get; set; }
        }
    }
}