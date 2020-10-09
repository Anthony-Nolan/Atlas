using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Mapping;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchService
    {
        Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest);
    }

    internal class SearchService : ISearchService
    {
        private const string LoggingPrefix = "Matching Algorithm: ";

        private readonly IHlaMetadataDictionary hlaMetadataDictionary;
        private readonly IMatchScoringService scoringService;
        private readonly IMatchingService matchingService;
        private readonly ILogger searchLogger;

        public SearchService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IMatchScoringService scoringService,
            IMatchingService matchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger
        )
        {
            this.scoringService = scoringService;
            this.matchingService = matchingService;
            this.searchLogger = searchLogger;
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        public async Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest)
        {
            var expansionTimer = searchLogger.RunTimed($"{LoggingPrefix}Expand patient HLA");
            var criteria = await GetMatchCriteria(matchingRequest);
            expansionTimer.Dispose();

            var matches = matchingService.GetMatches(criteria);

            var request = new StreamingMatchResultsScoringRequest
            {
                PatientHla = matchingRequest.SearchHlaData,
                MatchResults = matches,
                ScoringCriteria = matchingRequest.ScoringCriteria
            };
            
            // As matching phase 2 uses the same batch size as phase 1, which is expected to be very large - the result set is never expected to be large enough that scoring actually begins
            // before matching is complete. However, it wouldn't take much tweaking of batch sizes to enable scoring streaming to begin early.
            // If memory continues to be a concern on large datasets, it wouldn't be much work from here to stream results to file so we don't even need to store all results in memory! Though 
            // to do so would be to remove ranking of results, and may cause issues down the line where all results *do* need to be loaded into memory.
            var scoredMatches = await scoringService.StreamScoring(request);
            return scoredMatches.Select(MapSearchResultToApiSearchResult);
        }

        private async Task<AlleleLevelMatchCriteria> GetMatchCriteria(SearchRequest matchingRequest)
        {
            var matchCriteria = matchingRequest.MatchCriteria;
            var searchHla = matchingRequest.SearchHlaData.ToPhenotypeInfo();
            var criteriaMappings = await Task.WhenAll(
                MapLocusInformationToMatchCriteria(Locus.A, matchCriteria.LocusMismatchCriteria.A, searchHla.A),
                MapLocusInformationToMatchCriteria(Locus.B, matchCriteria.LocusMismatchCriteria.B, searchHla.B),
                MapLocusInformationToMatchCriteria(Locus.C, matchCriteria.LocusMismatchCriteria.C, searchHla.C),
                MapLocusInformationToMatchCriteria(Locus.Dqb1, matchCriteria.LocusMismatchCriteria.Dqb1, searchHla.Dqb1),
                MapLocusInformationToMatchCriteria(Locus.Drb1, matchCriteria.LocusMismatchCriteria.Drb1, searchHla.Drb1));

            return new AlleleLevelMatchCriteria
            {
                SearchType = matchingRequest.SearchDonorType.ToMatchingAlgorithmDonorType(),
                DonorMismatchCount = matchCriteria.DonorMismatchCount,
                LocusCriteria = new LociInfo<AlleleLevelLocusMatchCriteria>(
                    criteriaMappings[0],
                    criteriaMappings[1],
                    criteriaMappings[2],
                    null,
                    criteriaMappings[3],
                    criteriaMappings[4]
                ),
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
                AtlasDonorId = result.MatchResult.DonorInfo.DonorId,
                DonorType = result.MatchResult.DonorInfo.DonorType.ToAtlasClientModel(),

                MatchingResult = new MatchingResult
                {
                    TotalMatchCount = result.MatchResult.TotalMatchCount,
                    DonorHla = result.MatchResult.DonorInfo.HlaNames.ToPhenotypeInfoTransfer(),
                    TypedLociCount = result.MatchResult.TypedLociCount,
                },

                ScoringResult = new ScoringResult
                {
                    TotalMatchCount = result.ScoreResult?.AggregateScoreDetails.MatchCount ?? 0,
                    MatchCategory = result.ScoreResult?.AggregateScoreDetails.MatchCategory,
                    ConfidenceScore = result.ScoreResult?.AggregateScoreDetails.ConfidenceScore,
                    GradeScore = result.ScoreResult?.AggregateScoreDetails.GradeScore,
                    TypedLociCountAtScoredLoci = result.ScoreResult?.AggregateScoreDetails.TypedLociCount,
                    PotentialMatchCount = result.PotentialMatchCount,
                    ScoringResultsByLocus = new LociInfo<LocusSearchResult>().Map((l, _) => MapSearchResultToApiLocusSearchResult(result, l))
                        .ToLociInfoTransfer(),
                },
            };
        }

        private static LocusSearchResult MapSearchResultToApiLocusSearchResult(MatchAndScoreResult result, Locus locus)
        {
            var matchDetailsForLocus = result.MatchResult.MatchDetailsForLocus(locus);
            var scoreDetailsForLocus = result.ScoreResult?.ScoreDetailsForLocus(locus);

            // do not return a result if neither matching nor scoring was performed at this locus
            if (matchDetailsForLocus == null && scoreDetailsForLocus == null)
            {
                return default;
            }

            return new LocusSearchResult
            {
                MatchCount = matchDetailsForLocus?.MatchCount ?? scoreDetailsForLocus.MatchCount(),

                IsLocusMatchCountIncludedInTotal = matchDetailsForLocus != null,

                // scoring results
                IsLocusTyped = scoreDetailsForLocus?.IsLocusTyped,
                MatchGradeScore = scoreDetailsForLocus?.MatchGradeScore,
                MatchConfidenceScore = scoreDetailsForLocus?.MatchConfidenceScore,
                ScoreDetailsAtPositionOne = scoreDetailsForLocus?.ScoreDetailsAtPosition1,
                ScoreDetailsAtPositionTwo = scoreDetailsForLocus?.ScoreDetailsAtPosition2
            };
        }
    }
}