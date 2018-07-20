using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchResult = Nova.SearchAlgorithm.Client.Models.SearchResult;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly IDonorScoringService donorScoringService;
        private readonly IDonorMatchingService donorMatchingService;

        public SearchService(
            IHlaMatchingLookupService hlaMatchingLookupService, 
            IDonorScoringService donorScoringService,
            IDonorMatchingService donorMatchingService
            )
        {
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.donorScoringService = donorScoringService;
            this.donorMatchingService = donorMatchingService;
        }

        public async Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest)
        {
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, searchRequest.MatchCriteria.LocusMismatchA, searchRequest.SearchHlaData.LocusSearchHlaA),
                MapLocusInformationToMatchCriteria(Locus.B, searchRequest.MatchCriteria.LocusMismatchB, searchRequest.SearchHlaData.LocusSearchHlaB),
                MapLocusInformationToMatchCriteria(Locus.C, searchRequest.MatchCriteria.LocusMismatchC, searchRequest.SearchHlaData.LocusSearchHlaC),
                MapLocusInformationToMatchCriteria(Locus.Drb1, searchRequest.MatchCriteria.LocusMismatchDrb1, searchRequest.SearchHlaData.LocusSearchHlaDrb1),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, searchRequest.MatchCriteria.LocusMismatchDqb1, searchRequest.SearchHlaData.LocusSearchHlaDqb1));

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

            // TODO:NOVA-930 add scoring
            var patientHla = new PhenotypeInfo<string>
            {
                A_1 = searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla1,
                A_2 = searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla2,
                B_1 = searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla1,
                B_2 = searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla2,
                C_1 = searchRequest.SearchHlaData.LocusSearchHlaC?.SearchHla1,
                C_2 = searchRequest.SearchHlaData.LocusSearchHlaC?.SearchHla2,
                DQB1_1 = searchRequest.SearchHlaData.LocusSearchHlaDqb1?.SearchHla1,
                DQB1_2 = searchRequest.SearchHlaData.LocusSearchHlaDqb1?.SearchHla2,
                DRB1_1 = searchRequest.SearchHlaData.LocusSearchHlaDrb1?.SearchHla1,
                DRB1_2 = searchRequest.SearchHlaData.LocusSearchHlaDrb1?.SearchHla2
            };
            var scoredMatches = await donorScoringService.Score(patientHla, matches);
            
            return scoredMatches.Select(MapSearchResultToApiObject).OrderBy(r => r.MatchRank);
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus, 
            LocusMismatchCriteria mismatch, 
            LocusSearchHla searchHla)
        {
            if (mismatch == null)
            {
                return null;
            }

            var lookupResult = await Task.WhenAll(
                hlaMatchingLookupService.GetHlaLookupResult(locus.ToMatchLocus(), searchHla.SearchHla1),
                hlaMatchingLookupService.GetHlaLookupResult(locus.ToMatchLocus(), searchHla.SearchHla2));

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatch.MismatchCount,
                PGroupsToMatchInPositionOne = lookupResult[0].MatchingPGroups,
                PGroupsToMatchInPositionTwo = lookupResult[1].MatchingPGroups,
            };
        }

        private SearchResult MapSearchResultToApiObject(Common.Models.SearchResults.MatchAndScoreResult result)
        {
            return new SearchResult
            {
                DonorId = result.MatchResult.Donor.DonorId,
                DonorType = result.MatchResult.Donor.DonorType,
                Registry = result.MatchResult.Donor.RegistryCode,
                MatchRank = result.ScoreResult.TotalMatchRank,
                TotalMatchConfidence = result.ScoreResult.TotalMatchConfidenceScore,
                TotalMatchGrade = result.ScoreResult.TotalMatchGradeScore,
                TotalMatchCount = result.MatchResult.TotalMatchCount,
                TypedLociCount = result.MatchResult.TypedLociCount,
                SearchResultAtLocusA = ToSearchResultAtLocus(result, Locus.A),
                SearchResultAtLocusB = ToSearchResultAtLocus(result, Locus.B),
                SearchResultAtLocusC = ToSearchResultAtLocus(result, Locus.C),
                SearchResultAtLocusDqb1 = ToSearchResultAtLocus(result, Locus.Dqb1),
                SearchResultAtLocusDrb1 = ToSearchResultAtLocus(result, Locus.Drb1),
            };
        }

        private static LocusSearchResult ToSearchResultAtLocus(Common.Models.SearchResults.MatchAndScoreResult result, Locus locus)
        {
            var matchDetails = result.MatchResult.MatchDetailsForLocus(locus);
            if (matchDetails == null)
            {
                return null;
            }
            return new LocusSearchResult
            {
                IsLocusTyped = result.MatchResult.MatchDetailsForLocus(locus).IsLocusTyped,
                MatchCount = result.MatchResult.MatchDetailsForLocus(locus).MatchCount,
                MatchGrade = result.ScoreResult.ScoreDetailsForLocus(locus).MatchGradeScore,
                MatchConfidence = result.ScoreResult.ScoreDetailsForLocus(locus).MatchConfidenceScore
            };
        }
    }
}