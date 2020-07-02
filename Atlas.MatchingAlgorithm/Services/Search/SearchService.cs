using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
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
        Task<IEnumerable<MatchingAlgorithmResult>> Search(MatchingRequest matchingRequest);
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

        public async Task<IEnumerable<MatchingAlgorithmResult>> Search(MatchingRequest matchingRequest)
        {
            var criteria = await logger.RunTimedAsync(
                async () => await GetMatchCriteria(matchingRequest),
                $"{LoggingPrefix}Expanded patient HLA."
            );

            var matches = await logger.RunTimedAsync(
                async () => (await matchingService.GetMatches(criteria)).ToList(),
                $"{LoggingPrefix}Matching complete"
            );

            logger.SendTrace($"{LoggingPrefix}Matched {matches.Count} donors.");

            var scoredMatches = await logger.RunTimedAsync(
                async () =>
                {
                    var lociToExcludeFromAggregateScoring = matchingRequest.LociToExcludeFromAggregateScore.ToList();
                    // Scoring makes use of per-position map functionality of PhenotypeInfo, which requires all nested "LocusInfo"s to be non-null 
                    var patientHla = matchingRequest.SearchHlaData.MapByLocus(hla => hla ?? new LocusInfo<string>());
                    return await donorScoringService.ScoreMatchesAgainstHla(matches, patientHla, lociToExcludeFromAggregateScoring);
                },
                $"{LoggingPrefix}Scoring complete"
            );

            return scoredMatches.Select(MapSearchResultToApiSearchResult);
        }

        private async Task<AlleleLevelMatchCriteria> GetMatchCriteria(MatchingRequest matchingRequest)
        {
            var matchCriteria = matchingRequest.MatchCriteria;
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchCounts.A, matchingRequest.SearchHlaData.A),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchCounts.B, matchingRequest.SearchHlaData.B),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchCounts.C, matchingRequest.SearchHlaData.C),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchCounts.Drb1, matchingRequest.SearchHlaData.Drb1),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchCounts.Dqb1, matchingRequest.SearchHlaData.Dqb1));

            return new AlleleLevelMatchCriteria
            {
                SearchType = matchingRequest.SearchType,
                DonorMismatchCount = matchCriteria.DonorMismatchCount,
                LocusMismatchA = criteriaMappings[0],
                LocusMismatchB = criteriaMappings[1],
                LocusMismatchC = criteriaMappings[2],
                LocusMismatchDrb1 = criteriaMappings[3],
                LocusMismatchDqb1 = criteriaMappings[4]
            };
        }

        private async Task<AlleleLevelLocusMatchCriteria> MapLocusInformationToMatchCriteria(
            Locus locus,
            int? allowedMismatches,
            LocusInfo<string> searchHla)
        {
            if (allowedMismatches == null)
            {
                return null;
            }

            var searchTerm = new LocusInfo<string>(searchHla.Position1, searchHla.Position2);

            var metadata = await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(
                locus,
                searchTerm
            );

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = allowedMismatches.Value,
                PGroupsToMatchInPositionOne = metadata.Position1.MatchingPGroups,
                PGroupsToMatchInPositionTwo = metadata.Position2.MatchingPGroups
            };
        }

        private static MatchingAlgorithmResult MapSearchResultToApiSearchResult(MatchAndScoreResult result)
        {
            return new MatchingAlgorithmResult
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
                DonorHla = result.MatchResult.DonorInfo.HlaNames
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