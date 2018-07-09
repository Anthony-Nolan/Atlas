using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Scoring;
using Nova.SearchAlgorithm.Services.Matching;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<PotentialMatch>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly ICalculateScore calculateScore;
        private readonly IDonorMatchingService donorMatchingService;

        public SearchService(
            IMatchingDictionaryLookupService lookupService, 
            ICalculateScore calculateScore,
            IDonorMatchingService donorMatchingService
            )
        {
            this.lookupService = lookupService;
            this.calculateScore = calculateScore;
            this.donorMatchingService = donorMatchingService;
        }

        public async Task<IEnumerable<PotentialMatch>> Search(SearchRequest searchRequest)
        {
            var criteriaMappings = await Task.WhenAll(
                MapMismatchToMatchCriteria(Locus.A, searchRequest.MatchCriteria.LocusMismatchA),
                MapMismatchToMatchCriteria(Locus.B, searchRequest.MatchCriteria.LocusMismatchB),
                MapMismatchToMatchCriteria(Locus.C, searchRequest.MatchCriteria.LocusMismatchC),
                MapMismatchToMatchCriteria(Locus.Drb1, searchRequest.MatchCriteria.LocusMismatchDRB1),
                MapMismatchToMatchCriteria(Locus.Dqb1, searchRequest.MatchCriteria.LocusMismatchDQB1));

            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                RegistriesToSearch = searchRequest.RegistriesToSearch,
                DonorMismatchCount = (int) searchRequest.MatchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDRB1 = criteriaMappings[3],
                LocusMismatchDQB1 = criteriaMappings[4]
            };

            var matches = await donorMatchingService.Search(criteria);

            // TODO:NOVA-930 this won't update total match grade and confidence, only per-locus
            var scoredMatches = await Task.WhenAll(matches.Select(m => calculateScore.Score(criteria, m)));

            return scoredMatches.Select(MapSearchResultToApiObject).OrderBy(r => r.MatchRank);
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapMismatchToMatchCriteria(Locus locus, LocusMismatchCriteria mismatch)
        {
            if (mismatch == null)
            {
                return null;
            }

            var lookupResult = await Task.WhenAll(
                lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla1),
                lookupService.GetMatchingHla(locus.ToMatchLocus(), mismatch.SearchHla2));

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatch.MismatchCount,
                HlaNamesToMatchInPositionOne = lookupResult[0].MatchingPGroups,
                HlaNamesToMatchInPositionTwo = lookupResult[1].MatchingPGroups,
            };
        }

        private PotentialMatch MapSearchResultToApiObject(PotentialSearchResult result)
        {
            return new PotentialMatch
            {
                DonorId = result.Donor.DonorId,
                DonorType = result.Donor.DonorType,
                Registry = result.Donor.RegistryCode,
                MatchRank = result.MatchRank,
                TotalMatchConfidence = result.TotalMatchConfidence,
                TotalMatchGrade = result.TotalMatchGrade,
                TotalMatchCount = result.TotalMatchCount,
                TypedLociCount = result.TypedLociCount,
                MatchDetailsAtLocusA = result.MatchDetailsForLocus(Locus.A),
                MatchDetailsAtLocusB = result.MatchDetailsForLocus(Locus.B),
                MatchDetailsAtLocusC = result.MatchDetailsForLocus(Locus.C),
                MatchDetailsAtLocusDQB1 = result.MatchDetailsForLocus(Locus.Dqb1),
                MatchDetailsAtLocusDRB1 = result.MatchDetailsForLocus(Locus.Drb1)
            };
        }
    }
}