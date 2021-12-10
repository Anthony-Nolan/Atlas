using System;
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
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchService
    {
        Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest, DateTimeOffset? cutOffDate = null);
    }

    internal class SearchService : ISearchService
    {
        private const string LoggingPrefix = "Matching Algorithm: ";

        private readonly IMatchCriteriaMapper matchCriteriaMapper;
        private readonly IMatchScoringService scoringService;
        private readonly IMatchingService matchingService;
        private readonly ILogger searchLogger;
        private readonly IDonorReader donorReader;

        public SearchService(
            IMatchCriteriaMapper matchCriteriaMapper,
            IMatchScoringService scoringService,
            IMatchingService matchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            IDonorReader donorReader)
        {
            this.scoringService = scoringService;
            this.matchingService = matchingService;
            this.searchLogger = searchLogger;
            this.donorReader = donorReader;
            this.matchCriteriaMapper = matchCriteriaMapper;
        }

        public async Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest, DateTimeOffset? cutOffDate)
        {
            var expansionTimer = searchLogger.RunTimed($"{LoggingPrefix}Expand patient HLA");
            var criteria = await matchCriteriaMapper.MapRequestToAlleleLevelMatchCriteria(matchingRequest);
            expansionTimer.Dispose();

            var matches = matchingService.GetMatches(criteria, cutOffDate);

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
            var reifiedScoredMatches = scoredMatches.ToList();

            var donorLookupTimer = searchLogger.RunTimed($"{LoggingPrefix}Look up external donor ids");
            var donorLookup = await donorReader.GetDonors(reifiedScoredMatches.Select(r => r.MatchResult.DonorId));
            donorLookupTimer.Dispose();

            return reifiedScoredMatches.Select(scoredMatch => MapSearchResultToApiSearchResult(scoredMatch, donorLookup));
        }

        private static MatchingAlgorithmResult MapSearchResultToApiSearchResult(
            MatchAndScoreResult result,
            IReadOnlyDictionary<int, Donor> donorLookup)
        {
            var atlasDonorId = result.MatchResult.DonorInfo.DonorId;
            return new MatchingAlgorithmResult
            {
                AtlasDonorId = atlasDonorId,
                DonorCode = donorLookup[atlasDonorId].ExternalDonorCode,
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
                    PotentialMatchCount = result.ScoreResult?.AggregateScoreDetails.PotentialMatchCount ?? 0,
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
                ScoreDetailsAtPositionTwo = scoreDetailsForLocus?.ScoreDetailsAtPosition2,
                MatchCategory = scoreDetailsForLocus?.MatchCategory,
                Dpb1MismatchDirection = scoreDetailsForLocus?.Dpb1MismatchDirection
            };
        }
    }
}