using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.MatchingAlgorithm.Services.Debug
{
    public class MatchingResultNotificationsPeeker : ServiceBusPeeker<MatchingResultsNotification>
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