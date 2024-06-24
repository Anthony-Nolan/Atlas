using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Repositories
{
    public interface ISearchRequestRepository
    {
        Task TrackSearchRequestedEvent(SearchRequestedEvent requestedEvent);

        Task TrackMatchPredictionCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent);

        Task TrackMatchingAlgorithmCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);

        Task TrackSearchRequestCompletedEvent(SearchRequestCompletedEvent completedEvent);

        Task<SearchRequest> GetSearchRequestById(int id);
    }

    public class SearchRequestRepository : ISearchRequestRepository
    {
        private readonly ISearchTrackingContext context;

        private DbSet<SearchRequest> searchRequests => context.SearchRequests;

        public SearchRequestRepository(ISearchTrackingContext context)
        {
            this.context = context;
        }

        public async Task TrackSearchRequestedEvent(SearchRequestedEvent requestedEvent)
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

        public async Task TrackMatchPredictionCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var searchRequest = await GetSearchRequestById(matchPredictionCompletedEvent.SearchRequestId);

            searchRequest.MatchPrediction_IsSuccessful = matchPredictionCompletedEvent.CompletionDetails.IsSuccessful;
            searchRequest.MatchPrediction_FailureInfo_Json = matchPredictionCompletedEvent.CompletionDetails.FailureInfoJson;
            searchRequest.MatchPrediction_DonorsPerBatch = matchPredictionCompletedEvent.CompletionDetails.DonorsPerBatch;
            searchRequest.MatchPrediction_TotalNumberOfBatches = matchPredictionCompletedEvent.CompletionDetails.TotalNumberOfBatches;

            await context.SaveChangesAsync();
        }

        public async Task TrackMatchingAlgorithmCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var searchRequest = await GetSearchRequestById(matchingAlgorithmCompletedEvent.SearchRequestId);

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
            searchRequest.MatchingAlgorithm_ResultsSentTimeUTC = matchingAlgorithmCompletedEvent.ResultsSentTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task TrackSearchRequestCompletedEvent(SearchRequestCompletedEvent completedEvent)
        {
            var searchRequest = await GetSearchRequestById(completedEvent.SearchRequestId);

            searchRequest.ResultsSent = completedEvent.ResultsSent;
            searchRequest.ResultsSentTimeUTC = completedEvent.ResultsSentTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task<SearchRequest> GetSearchRequestById(int id)
        {
            var searchRequest = await searchRequests.FirstOrDefaultAsync(x => x.Id == id);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with id {id} not found");
            }

            return searchRequest;
        }
    }
}
