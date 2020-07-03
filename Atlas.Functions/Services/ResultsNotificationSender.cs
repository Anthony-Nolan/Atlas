using System.Text;
using System.Threading.Tasks;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Settings;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface ISearchCompletionMessageSender
    {
        Task PublishResultsMessage(SearchResultSet searchResultSet);
        Task PublishFailureMessage(string searchId, string failureMessage);
    }

    internal class SearchCompletionMessageSender : ISearchCompletionMessageSender
    {
        private readonly string connectionString;
        private readonly string resultsNotificationTopicName;

        public SearchCompletionMessageSender(IOptions<MessagingServiceBusSettings> messagingServiceBusSettings)
        {
            connectionString = messagingServiceBusSettings.Value.ConnectionString;
            resultsNotificationTopicName = messagingServiceBusSettings.Value.SearchResultsTopic;
        }

        /// <inheritdoc />
        public async Task PublishResultsMessage(SearchResultSet searchResultSet)
        {
            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = true,
                HlaNomenclatureVersion = searchResultSet.HlaNomenclatureVersion,
                NumberOfResults = searchResultSet.TotalResults,
                ResultsFileName = searchResultSet.ResultsFileName,
                SearchRequestId = searchResultSet.SearchRequestId,
                BlobStorageContainerName = searchResultSet.BlobStorageContainerName,
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        /// <inheritdoc />
        public async Task PublishFailureMessage(string searchId, string failureMessage)
        {
            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = false,
                SearchRequestId = searchId,
                FailureMessage = failureMessage
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        private async Task SendNotificationMessage(SearchResultsNotification searchResultsNotification)
        {
            var json = JsonConvert.SerializeObject(searchResultsNotification);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            var client = new TopicClient(connectionString, resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}