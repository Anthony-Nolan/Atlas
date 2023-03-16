using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using System.Reflection;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface IMatchingFailureNotificationSender
    {
        /// <param name="searchRequestId"></param>
        /// <param name="attemptNumber">The number of times this <paramref name="searchRequestId"/> has been attempted, including the current attempt.</param>
        /// <param name="remainingRetriesCount">The number of times this <paramref name="searchRequestId"/> will be retried until it completes successfully.</param>
        /// <param name="validationError"></param>
        Task SendFailureNotification(string searchRequestId, int attemptNumber, int remainingRetriesCount, string validationError = null);
    }

    public class MatchingFailureNotificationSender : IMatchingFailureNotificationSender
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        public MatchingFailureNotificationSender(ISearchServiceBusClient searchServiceBusClient, IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
        }

        public async Task SendFailureNotification(string searchRequestId, int attemptNumber, int remainingRetriesCount, string validationError = null)
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
                SearchRequestId = searchRequestId,
                MatchingAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion(),
                ValidationError = failureInfo.ValidationError,
                FailureInfo = failureInfo
            };

            await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
        }
    }
}
