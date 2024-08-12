using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.MatchingAlgorithm.Services.Debug
{
    internal class MatchingResultNotificationsPeeker : ServiceBusPeeker<MatchingResultsNotification>
    {
        public MatchingResultNotificationsPeeker(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName) : base(factory, topicName, subscriptionName)
        {
        }
    }
}