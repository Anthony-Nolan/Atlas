using Atlas.Client.Models.Search.Results;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.Functions.Services.Debug
{
    public interface IRepeatSearchResultNotificationsPeeker : IServiceBusPeeker<SearchResultsNotification>
    {
    }

    internal class RepeatSearchResultNotificationsPeeker : ServiceBusPeeker<SearchResultsNotification>, IRepeatSearchResultNotificationsPeeker
    {
        public RepeatSearchResultNotificationsPeeker(
            IMessageReceiverFactory factory, string connectionString, string topicName, string subscriptionName)
            : base(factory, connectionString, topicName, subscriptionName)
        {
        }
    }
}