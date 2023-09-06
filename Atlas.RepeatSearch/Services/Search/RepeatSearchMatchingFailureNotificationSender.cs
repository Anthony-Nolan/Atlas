using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Models;
using System.Reflection;
using System.Threading.Tasks;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchMatchingFailureNotificationSender
    {
        Task SendFailureNotification(IdentifiedRepeatSearchRequest repeatSearchRequest, int attemptNumber, int remainingRetriesCount, string validationError = null);
    }

    public class RepeatSearchMatchingFailureNotificationSender : IRepeatSearchMatchingFailureNotificationSender
    {
        private readonly IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        public RepeatSearchMatchingFailureNotificationSender(IRepeatSearchServiceBusClient repeatSearchServiceBusClient, IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
        }

        public async Task SendFailureNotification(IdentifiedRepeatSearchRequest repeatSearchRequest, int attemptNumber, int remainingRetriesCount, string validationError = null)
        {
            var failureInfo = new MatchingAlgorithmFailureInfo
            {
                ValidationError = validationError,
                AttemptNumber = attemptNumber,
                RemainingRetriesCount = remainingRetriesCount
            };

            var notification = new MatchingResultsNotification
            {
                WasSuccessful = false,
                SearchRequestId = repeatSearchRequest.OriginalSearchId,
                RepeatSearchRequestId = repeatSearchRequest.RepeatSearchId,
                SearchRequest = repeatSearchRequest.RepeatSearchRequest.SearchRequest,
                MatchingAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion(),
                FailureInfo = failureInfo
            };

            await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
        }
    }
}
