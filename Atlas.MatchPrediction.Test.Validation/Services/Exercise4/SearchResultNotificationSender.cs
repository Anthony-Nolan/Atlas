using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.ServiceBus;
using Atlas.ManualTesting.Common.Services;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4
{
    public interface ISearchResultNotificationSender
    {
        /// <summary>
        /// Intended to be used to prompt re-download of successful search results
        /// </summary>
        Task SendSuccessNotifications(IEnumerable<string> searchRequestIds);
    }

    internal class SearchResultNotificationSender : MessageSender<SearchResultsNotification>, ISearchResultNotificationSender
    {
        public SearchResultNotificationSender(
            IMessageBatchPublisher<SearchResultsNotification> messagePublisher,
            string resultsBlobContainerName) : base(messagePublisher, resultsBlobContainerName)
        {
        }

        public async Task SendSuccessNotifications(IEnumerable<string> searchRequestIds)
        {
            await BuildAndSendMessages(searchRequestIds);
        }

        /// <summary>
        /// Builds a basic search results notification message and assumes the search was successful.
        /// </summary>
        protected override SearchResultsNotification BuildMessage(string requestId, string resultsBlobContainerName)
        {
            return new SearchResultsNotification
            {
                SearchRequestId = requestId,
                WasSuccessful = true,
                BlobStorageContainerName = resultsBlobContainerName,
                ResultsFileName = $"{requestId}.json",
                ResultsBatched = false
            };
        }
    }
}