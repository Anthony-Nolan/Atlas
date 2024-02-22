using Atlas.Client.Models.Search.Results;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.Functions.Services.Debug
{
    internal class SearchResultNotificationsPeeker : ServiceBusPeeker<SearchResultsNotification>
    {
        public SearchResultNotificationsPeeker(IMessageReceiverFactory factory, string connectionString, string topicName, string subscriptionName)
            : base(factory, connectionString, topicName, subscriptionName)
        {
        }
    }
}