using Atlas.Client.Models.Search.Results;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.Functions.Services.Debug
{
    public interface ISearchResultNotificationsPeeker : IServiceBusPeeker<SearchResultsNotification>
    {
    }

    internal class SearchResultNotificationsPeeker : ServiceBusPeeker<SearchResultsNotification>, ISearchResultNotificationsPeeker
    {
        public SearchResultNotificationsPeeker(
            IMessageReceiverFactory factory, string topicName, string subscriptionName)
            : base(factory, topicName, subscriptionName)
        {
        }
    }
}