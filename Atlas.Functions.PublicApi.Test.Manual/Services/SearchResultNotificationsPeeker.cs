using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Functions.PublicApi.Test.Manual.Models;
using Atlas.Functions.PublicApi.Test.Manual.Services.ServiceBus;

namespace Atlas.Functions.PublicApi.Test.Manual.Services
{
    public interface ISearchResultNotificationsPeeker
    {
        Task<IEnumerable<string>> GetIdsOfFailedSearches(PeekRequest peekRequest);
    }

    internal class SearchResultNotificationsPeeker : ISearchResultNotificationsPeeker
    {
        private readonly IMessagesPeeker<SearchResultsNotification> messagesReceiver;

        public SearchResultNotificationsPeeker(IMessagesPeeker<SearchResultsNotification> messagesReceiver)
        {
            this.messagesReceiver = messagesReceiver;
        }

        public async Task<IEnumerable<string>> GetIdsOfFailedSearches(PeekRequest peekRequest)
        {
            var notifications = await messagesReceiver.Peek(peekRequest);

            return notifications
                .Where(n => !n.DeserializedBody.WasSuccessful)
                .Select(n => n.DeserializedBody.SearchRequestId)
                .Distinct();
        }
    }
}