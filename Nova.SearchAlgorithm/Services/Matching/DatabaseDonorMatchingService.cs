using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDatabaseDonorMatchingService
    {
        
        /// <summary>
        /// Searches the pre-processed matching data for matches at the specified loci
        /// Performs filtering against loci and total mismatch counts
        /// </summary>
        /// <returns>
        /// A collection of PotentialSearchResults, with donor id populated. MatchDetails will be populated only for the specified loci
        /// </returns>
        Task<IEnumerable<PotentialSearchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IEnumerable<Locus> loci);
    }
    
    public class DatabaseDonorDonorMatchingService: IDatabaseDonorMatchingService
    {
        private readonly IDonorSearchRepository donorSearchRepository;

        public DatabaseDonorDonorMatchingService(IDonorSearchRepository donorSearchRepository)
        {
            this.donorSearchRepository = donorSearchRepository;
        }
        
        public async Task<IEnumerable<PotentialSearchResult>> FindMatchesForLoci(AlleleLevelMatchCriteria criteria, IEnumerable<Locus> loci)
        {
            if (loci.Contains(Locus.Dpb1) || loci.Contains(Locus.Dqb1) || loci.Contains(Locus.C))
            {
                // Currently the logic here will not suffice for these loci
                // Donors with no typing for the locus should count as potential matches, but will not be returned by a search of the matching table
                throw new NotImplementedException();
            }
            
            var results = await Task.WhenAll(loci.Select(l => FindMatchesAtLocus(criteria.SearchType, criteria.RegistriesToSearch, l, criteria.MatchCriteriaForLocus(l))));

            var matches = results
                .SelectMany(r => r)
                .GroupBy(m => m.Key)
                .Select(matchesForDonor =>
                {
                    var donorId = matchesForDonor.Key;
                    var result = new PotentialSearchResult
                    {
                        DonorId = donorId,
                    };
                    foreach (var locus in loci)
                    {
                        var matchesAtLocus = matchesForDonor.FirstOrDefault(m => m.Value.Locus == locus);
                        var locusMatchDetails = matchesAtLocus.Value != null ? matchesAtLocus.Value.Match : new LocusMatchDetails { MatchCount = 0 };
                        result.SetMatchDetailsForLocus(locus, locusMatchDetails);
                    }
                    return result;
                })
                .Where(m => m.TotalMatchCount >= (loci.Count() * 2) - criteria.DonorMismatchCount)
                .Where(m => loci.All(l => m.MatchDetailsForLocus(l).MatchCount >= 2 - criteria.MatchCriteriaForLocus(l).MismatchCount));
            
            return matches;
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
                DonorId = group.Key,
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

        private class DonorAndMatchForLocus
        {
            public LocusMatchDetails Match { get; set; }
            public int DonorId { get; set; }
            public Locus Locus { get; set; }
        }
    }
}