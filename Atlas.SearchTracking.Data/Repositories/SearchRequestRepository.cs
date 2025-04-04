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

        Task<SearchRequest> GetSearchRequestByGuid(Guid searchRequestId);

        Task<int> GetSearchRequestIdByGuid(Guid searchRequestId);
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
                RequestTimeUtc = requestedEvent.RequestTimeUtc,
                IsMatchPredictionRun = requestedEvent.IsMatchPredictionRun,
                AreBetterMatchesIncluded = requestedEvent.AreBetterMatchesIncluded,
                DonorRegistryCodes = requestedEvent.DonorRegistryCodes
            };

            searchRequests.Add(searchRequest);
            await context.SaveChangesAsync();
        }

        public async Task TrackMatchPredictionCompletedEvent(MatchPredictionCompletedEvent matchPredictionCompletedEvent)
        {
            var searchRequest = await GetSearchRequestByGuid(matchPredictionCompletedEvent.SearchRequestId);

            searchRequest.MatchPrediction_IsSuccessful = matchPredictionCompletedEvent.CompletionDetails.IsSuccessful;
            searchRequest.MatchPrediction_FailureInfo_Message = matchPredictionCompletedEvent.CompletionDetails.FailureInfo?.Message;
            searchRequest.MatchPrediction_FailureInfo_ExceptionStacktrace = matchPredictionCompletedEvent.CompletionDetails.FailureInfo?.ExceptionStacktrace;
            searchRequest.MatchPrediction_FailureInfo_Type = matchPredictionCompletedEvent.CompletionDetails.FailureInfo?.Type;
            searchRequest.MatchPrediction_DonorsPerBatch = matchPredictionCompletedEvent.CompletionDetails.DonorsPerBatch;
            searchRequest.MatchPrediction_TotalNumberOfBatches = matchPredictionCompletedEvent.CompletionDetails.TotalNumberOfBatches;

            await context.SaveChangesAsync();
        }

        public async Task TrackMatchingAlgorithmCompletedEvent(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent)
        {
            var searchRequest = await GetSearchRequestByGuid(matchingAlgorithmCompletedEvent.SearchIdentifier);

            searchRequest.MatchingAlgorithm_IsSuccessful = matchingAlgorithmCompletedEvent.CompletionDetails.IsSuccessful;
            searchRequest.MatchingAlgorithm_FailureInfo_Message = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.Message;
            searchRequest.MatchingAlgorithm_FailureInfo_ExceptionStacktrace = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.ExceptionStacktrace;
            searchRequest.MatchingAlgorithm_FailureInfo_Type = matchingAlgorithmCompletedEvent.CompletionDetails.FailureInfo?.Type;
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
            searchRequest.MatchingAlgorithm_ResultsSentTimeUtc = matchingAlgorithmCompletedEvent.ResultsSentTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task TrackSearchRequestCompletedEvent(SearchRequestCompletedEvent completedEvent)
        {
            var searchRequest = await GetSearchRequestByGuid(completedEvent.SearchRequestId);

            searchRequest.ResultsSent = completedEvent.ResultsSent;
            searchRequest.ResultsSentTimeUtc = completedEvent.ResultsSentTimeUtc;

            await context.SaveChangesAsync();
        }

        public async Task<SearchRequest> GetSearchRequestByGuid(Guid searchRequestId)
        {
            var searchRequest = await searchRequests.FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with Guid {searchRequestId} not found");
            }

            return searchRequest;
        }

        public async Task<int> GetSearchRequestIdByGuid(Guid searchRequestId)
        {
            var searchRequest = await searchRequests.FirstOrDefaultAsync(x => x.SearchRequestId == searchRequestId);

            if (searchRequest == null)
            {
                throw new Exception($"Search request with Guid {searchRequestId} not found");
            }

            return searchRequest.Id;
        }
    }
}
