using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Common.Results;

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
        private readonly IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher;

        public SearchService(
            IMatchCriteriaMapper matchCriteriaMapper,
            IMatchScoringService scoringService,
            IMatchingService matchingService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher)
        {
            this.scoringService = scoringService;
            this.matchingService = matchingService;
            this.searchLogger = searchLogger;
            this.matchCriteriaMapper = matchCriteriaMapper;
            this.matchingAlgorithmSearchTrackingDispatcher = matchingAlgorithmSearchTrackingDispatcher;
        }

        public async Task<IEnumerable<MatchingAlgorithmResult>> Search(SearchRequest matchingRequest, DateTimeOffset? cutOffDate)
        {
            var expansionTimer = searchLogger.RunTimed($"{LoggingPrefix}Expand patient HLA");
            var criteria = await matchCriteriaMapper.MapRequestToAlleleLevelMatchCriteria(matchingRequest);
            expansionTimer.Dispose();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(criteria);
            searchLogger.SendTrace(
                $"Split into {splitSearch.Count} sub-searches: {splitSearch.Select(s => $"{s.LocusCriteria.A?.MismatchCount}{s.LocusCriteria.B?.MismatchCount}{s.LocusCriteria.Drb1?.MismatchCount}{s.LocusCriteria.C?.MismatchCount}{s.LocusCriteria.Dqb1?.MismatchCount}").StringJoin("|")}");

            var matches = RunSubSearches(splitSearch, cutOffDate, new NonHlaFilteringCriteria { RegistryCodes = matchingRequest.DonorRegistryCodes});

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
            var reifiedScoredMatches = scoredMatches.DistinctBy(m => m.MatchResult.DonorId).ToList();
            searchLogger.SendTrace($"Via {splitSearch.Count} sub-searches, matched {reifiedScoredMatches.Count} donors total.");

            var donorLookup = GetDonorLookup(reifiedScoredMatches);
            return reifiedScoredMatches.Select(scoredMatch => MapSearchResultToApiSearchResult(scoredMatch, donorLookup));
        }

        private async IAsyncEnumerable<MatchResult> RunSubSearches(List<AlleleLevelMatchCriteria> splitSearch, DateTimeOffset? cutOffDate, NonHlaFilteringCriteria nonHlaFilteringCriteria)
        {
            await matchingAlgorithmSearchTrackingDispatcher.ProcessCoreMatchingStarted();

            foreach (var subSearch in splitSearch)
            {
                var criteria = new MatchCriteria { AlleleLevelMatchCriteria = subSearch, NonHlaFilteringCriteria = nonHlaFilteringCriteria };
                var subSearchResults = matchingService.GetMatches(criteria, cutOffDate);
                await foreach (var result in subSearchResults)
                {
                    yield return result;
                }
            }

            await matchingAlgorithmSearchTrackingDispatcher.ProcessCoreMatchingEnded();
        }

        private MatchingAlgorithmResult MapSearchResultToApiSearchResult(
            MatchAndScoreResult result,
            IReadOnlyDictionary<int, DonorLookupInfo> donorLookup)
        {
            var atlasDonorId = result.MatchResult.DonorInfo.DonorId;

            if (!donorLookup.TryGetValue(atlasDonorId, out var donor))
            {
                var message = $"Donor with id {result.MatchResult.DonorInfo.DonorId} can't be found in donorLookup dictionary";
                searchLogger.SendTrace(message);
                throw new KeyNotFoundException(message);
            }

            return new MatchingAlgorithmResult
            {
                AtlasDonorId = atlasDonorId,
                DonorCode = donor.ExternalDonorCode,
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

                MatchingDonorInfo = new MatchingDonorInfo
                {
                    DonorType = result.MatchResult.DonorInfo.DonorType.ToAtlasClientModel(),
                    ExternalDonorCode = donor.ExternalDonorCode,
                    EthnicityCode = donor.EthnicityCode,
                    RegistryCode = donor.RegistryCode
                }
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
                MismatchDirection = scoreDetailsForLocus?.MismatchDirection
            };
        }

        public static Dictionary<int, DonorLookupInfo> GetDonorLookup(List<MatchAndScoreResult> reifiedScoredMatches)
        {
            return reifiedScoredMatches.ToDictionary(r => r.MatchResult.DonorId, r => r.MatchResult.DonorInfo.ToDonorLookupInfo());
        }
    }
}