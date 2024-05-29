using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface ISearchRequestRepository
    {
        Task Create(SearchRequestedEvent requestedEvent);

        Task UpdateMatchPredictionCompleted(MatchPredictionCompletedEvent matchPredictionCompletedEvent);

        Task UpdateMatchingAlgorithmCompleted(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);
    }

    public class SearchRequestRepository : ISearchRequestRepository
    {
        private readonly SearchTrackingContext context;

        private DbSet<SearchRequest> searchRequests => context.SearchRequests;

        public SearchRequestRepository(SearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task Create(SearchRequestedEvent requestedEvent)
        {
            var searchRequest = new SearchRequest
            {
                SearchRequestId = requestedEvent.SearchRequestId,
                IsRepeatSearch = requestedEvent.IsRepeatSearch,
                OriginalSearchRequestId = requestedEvent.OriginalSearchRequestId,
                RepeatSearchCutOffDate = requestedEvent.RepeatSearchCutOffDate,
                RequestJson = requestedEvent.RequestJson,
                SearchCriteria = requestedEvent.SearchCriteria,
                DonorType = requestedEvent.DonorType,
                RequestTimeUTC = requestedEvent.RequestTimeUtc
            };

            searchRequests.Add(searchRequest);
            await context.SaveChangesAsync();
        }

        public async Task UpdateMatchPredictionCompleted(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var searchRequest = await searchRequests.FindAsync(matchPredictionCompletedEvent.SearchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with id {matchPredictionCompletedEvent.SearchRequestId} not found");
            }

            searchRequest.MatchPrediction_IsSuccessful = matchPredictionCompletedEvent.CompletionDetails.IsSuccessful;
            searchRequest.MatchPrediction_FailureInfo_Json = matchPredictionCompletedEvent.CompletionDetails.FailureInfoJson;
            searchRequest.MatchPrediction_DonorsPerBatch = matchPredictionCompletedEvent.CompletionDetails.DonorsPerBatch;
            searchRequest.MatchPrediction_TotalNumberOfBatches = matchPredictionCompletedEvent.CompletionDetails.TotalNumberOfBatches;

            await context.SaveChangesAsync();
        }

        public async Task UpdateMatchingAlgorithmCompleted(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var searchRequest = await searchRequests.FindAsync(matchingAlgorithmCompletedEvent.SearchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with id {matchingAlgorithmCompletedEvent.SearchRequestId} not found");
            }

            searchRequest.MatchingAlgorithm_IsSuccessful = matchingAlgorithmCompletedEvent.CompletionDetails.IsSuccessful;
            searchRequest.MatchingAlgorithm_FailureInfo_Json = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfoJson;
            searchRequest.MatchingAlgorithm_TotalAttemptsNumber = matchingAlgorithmCompletedEvent.CompletionDetails.TotalAttemptsNumber;
            searchRequest.MatchingAlgorithm_NumberOfMatching = matchingAlgorithmCompletedEvent.CompletionDetails.NumberOfMatching;
            searchRequest.MatchingAlgorithm_NumberOfResults = matchingAlgorithmCompletedEvent.CompletionDetails.NumberOfResults;

            if (matchingAlgorithmCompletedEvent.CompletionDetails.RepeatSearchResultsDetails != null)
            {
                searchRequest.MatchingAlgorithm_RepeatSearch_AddedResultCount =
                    matchingAlgorithmCompletedEvent.CompletionDetails.RepeatSearchResultsDetails.AddedResultCount;
                searchRequest.MatchingAlgorithm_RepeatSearch_RemovedResultCount =
                    matchingAlgorithmCompletedEvent.CompletionDetails.RepeatSearchResultsDetails.RemovedResultCount;
                searchRequest.MatchingAlgorithm_RepeatSearch_UpdatedResultCount =
                    matchingAlgorithmCompletedEvent.CompletionDetails.RepeatSearchResultsDetails.UpdatedResultCount;
            }

            searchRequest.MatchingAlgorithm_HlaNomenclatureVersion = matchingAlgorithmCompletedEvent.HlaNomenclatureVersion;
            searchRequest.MatchingAlgorithm_ResultsSent = matchingAlgorithmCompletedEvent.ResultsSent;
            searchRequest.ResultsSent = matchingAlgorithmCompletedEvent.ResultsSent;
            searchRequest.MatchingAlgorithm_ResultsSentTimeUTC = matchingAlgorithmCompletedEvent.ResultsSentTimeUtc;

            await context.SaveChangesAsync();
        }
    }
}
