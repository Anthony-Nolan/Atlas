using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Services.Scoring;
using SearchResult = Nova.SearchAlgorithm.Client.Models.SearchResult;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private readonly IMatchingDictionaryLookupService lookupService;
        private readonly IDonorScoringService donorScoringService;
        private readonly IDonorMatchingService donorMatchingService;

        public SearchService(
            IMatchingDictionaryLookupService lookupService, 
            IDonorScoringService donorScoringService,
            IDonorMatchingService donorMatchingService
            )
        {
            this.lookupService = lookupService;
            this.donorScoringService = donorScoringService;
            this.donorMatchingService = donorMatchingService;
        }

        public async Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest)
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
            var scoredMatches = await donorScoringService.Score(criteria, matches);

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
                TotalMatchConfidence = result.ScoreResult.TotalMatchConfidence,
                TotalMatchGrade = result.ScoreResult.TotalMatchGrade,
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
                MatchGrade = result.ScoreResult.ScoreDetailsForLocus(locus).MatchGrade,
                MatchConfidence = result.ScoreResult.ScoreDetailsForLocus(locus).MatchConfidence
            };
        }
    }
}