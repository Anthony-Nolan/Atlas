using System.Text;
using System.Threading.Tasks;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Settings;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface IResultsNotificationSender
    {
        Task PublishResultsNotification(SearchResultSet searchResultSet);
    }

    internal class ResultsNotificationSender : IResultsNotificationSender
    {
        private readonly string connectionString;
        private readonly string resultsNotificationTopicName;

        public ResultsNotificationSender(IOptions<MessagingServiceBusSettings> messagingServiceBusSettings)
        {
            connectionString = messagingServiceBusSettings.Value.ConnectionString;
            resultsNotificationTopicName = messagingServiceBusSettings.Value.SearchResultsTopic;
        }

        /// <inheritdoc />
        public async Task PublishResultsNotification(SearchResultSet searchResultSet)
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

            var json = JsonConvert.SerializeObject(searchResultsNotification);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            var client = new TopicClient(connectionString, resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}