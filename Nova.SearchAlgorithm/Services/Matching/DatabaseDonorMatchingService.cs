using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDatabaseDonorMatchingService
    {
        
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci
        /// </summary>
        /// <returns>
        /// A PotentialSearchResult object, with donor details populated. MatchDetails will be populated only for the specified loci
        /// </returns>
        Task<IEnumerable<PotentialSearchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IReadOnlyList<Locus> loci);
    }
    
    public class DatabaseDonorDonorMatchingService: IDatabaseDonorMatchingService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorSearchRepository donorSearchRepository;
        private readonly IDonorInspectionRepository donorInspectionRepository;

        public DatabaseDonorDonorMatchingService(IDonorSearchRepository donorSearchRepository, IDonorInspectionRepository donorInspectionRepository, IMatchingDictionaryLookupService lookupService)
        {
            this.donorSearchRepository = donorSearchRepository;
            this.donorInspectionRepository = donorInspectionRepository;
            this.lookupService = lookupService;
        }
        
        public async Task<IEnumerable<PotentialSearchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IReadOnlyList<Locus> loci)
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
                Donor = group.First()?.Donor ?? new DonorResult {DonorId = group.Key},
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