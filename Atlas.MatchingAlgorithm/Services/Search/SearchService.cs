using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest);
    }

    public class SearchService : ISearchService
    {
        private const string LoggingPrefix = "Matching Algorithm: ";

        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IDonorScoringService donorScoringService;
        private readonly IMatchingService matchingService;
        private readonly ILogger logger;

        public SearchService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IDonorScoringService donorScoringService,
            IMatchingService matchingService,
            ILogger logger
        )
        {
            this.donorScoringService = donorScoringService;
            this.matchingService = matchingService;
            this.logger = logger;
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public async Task<IEnumerable<SearchResult>> Search(SearchRequest searchRequest)
        {
            var criteria = await TimingLogger.RunTimedAsync(
                async () => await GetMatchCriteria(searchRequest),
                $"{LoggingPrefix}Expanded patient HLA.",
                logger
            );

            var matches = await TimingLogger.RunTimedAsync(
                async () => (await matchingService.GetMatches(criteria)).ToList(),
                $"{LoggingPrefix}Matching complete",
                logger
            );

            logger.SendTrace($"{LoggingPrefix}Matched {matches.Count} donors.");

            var scoredMatches = await TimingLogger.RunTimedAsync(
                async () =>
                {
                    var lociToExcludeFromAggregateScoring = searchRequest.LociToExcludeFromAggregateScore.ToList();
                    var patientHla = searchRequest.SearchHlaData.ToPhenotypeInfo();
                    return await donorScoringService.ScoreMatchesAgainstHla(matches, patientHla, lociToExcludeFromAggregateScoring);
                },
                $"{LoggingPrefix}Scoring complete",
                logger
            );
            
            return scoredMatches.Select(MapSearchResultToApiSearchResult);
        }

        private async Task<AlleleLevelMatchCriteria> GetMatchCriteria(SearchRequest searchRequest)
        {
            var matchCriteria = searchRequest.MatchCriteria;
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchA, searchRequest.SearchHlaData.LocusSearchHlaA),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchB, searchRequest.SearchHlaData.LocusSearchHlaB),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchC, searchRequest.SearchHlaData.LocusSearchHlaC),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchDrb1, searchRequest.SearchHlaData.LocusSearchHlaDrb1),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchDqb1, searchRequest.SearchHlaData.LocusSearchHlaDqb1));

            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchRequest.SearchType,
                DonorMismatchCount = (int) matchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDrb1 = criteriaMappings[3],
                LocusMismatchDqb1 = criteriaMappings[4]
            };
            return criteria;
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus,
            LocusMismatchCriteria mismatchCriteria,
            LocusSearchHla searchHla)
        {
            if (mismatchCriteria == null)
            {
                return null;
            }

            var searchTerm = new LocusInfo<string>(searchHla.SearchHla1, searchHla.SearchHla2);

            var metadata = await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(
                locus,
                searchTerm
            );

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatchCriteria.MismatchCount,
                PGroupsToMatchInPositionOne = metadata.Position1.MatchingPGroups,
                PGroupsToMatchInPositionTwo = metadata.Position2.MatchingPGroups
            };
        }

        private static SearchResult MapSearchResultToApiSearchResult(MatchAndScoreResult result)
        {
            return new SearchResult
            {
                DonorId = result.MatchResult.DonorInfo.DonorId,
                DonorType = result.MatchResult.DonorInfo.DonorType,
                MatchCategory = result.ScoreResult.AggregateScoreDetails.MatchCategory,
                ConfidenceScore = result.ScoreResult.AggregateScoreDetails.ConfidenceScore,
                GradeScore = result.ScoreResult.AggregateScoreDetails.GradeScore,
                TypedLociCount = result.ScoreResult.AggregateScoreDetails.TypedLociCount,
                TotalMatchCount = result.MatchResult.TotalMatchCount,
                PotentialMatchCount = result.PotentialMatchCount,
                SearchResultAtLocusA = MapSearchResultToApiLocusSearchResult(result, Locus.A),
                SearchResultAtLocusB = MapSearchResultToApiLocusSearchResult(result, Locus.B),
                SearchResultAtLocusC = MapSearchResultToApiLocusSearchResult(result, Locus.C),
                SearchResultAtLocusDpb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Dpb1),
                SearchResultAtLocusDqb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Dqb1),
                SearchResultAtLocusDrb1 = MapSearchResultToApiLocusSearchResult(result, Locus.Drb1),
            };
        }

        private static LocusSearchResult MapSearchResultToApiLocusSearchResult(MatchAndScoreResult result, Locus locus)
        {
            var matchDetailsForLocus = result.MatchResult.MatchDetailsForLocus(locus);
            var scoreDetailsForLocus = result.ScoreResult.ScoreDetailsForLocus(locus);

            return new LocusSearchResult
            {
                IsLocusTyped = scoreDetailsForLocus.IsLocusTyped,
                MatchCount = matchDetailsForLocus?.MatchCount ?? scoreDetailsForLocus.MatchCount(),
                IsLocusMatchCountIncludedInTotal = matchDetailsForLocus != null,
                MatchGradeScore = scoreDetailsForLocus.MatchGradeScore,
                MatchConfidenceScore = scoreDetailsForLocus.MatchConfidenceScore,
                ScoreDetailsAtPositionOne = scoreDetailsForLocus.ScoreDetailsAtPosition1,
                ScoreDetailsAtPositionTwo = scoreDetailsForLocus.ScoreDetailsAtPosition2
            };
        }
    }
}