using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.RepeatSearch.Services.Debug
{
    internal class MatchingResultNotificationsPeeker : ServiceBusPeeker<MatchingResultsNotification>
    {
        public MatchingResultNotificationsPeeker(
            IMessageReceiverFactory factory,
            string connectionString,
            string topicName,
            string subscriptionName) : base(factory, connectionString, topicName, subscriptionName)
        {
        }
    }
}