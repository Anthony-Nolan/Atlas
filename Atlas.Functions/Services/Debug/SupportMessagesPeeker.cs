using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;

namespace Atlas.Functions.Services.Debug
{
    internal class AlertsPeeker : ServiceBusPeeker<Alert>
    {
        public AlertsPeeker(IMessageReceiverFactory factory, string connectionString, string topicName, string subscriptionName)
            : base(factory, connectionString, topicName, subscriptionName)
        {
        }
    }

    internal class NotificationsPeeker : ServiceBusPeeker<Notification>
    {
        public NotificationsPeeker(IMessageReceiverFactory factory, string connectionString, string topicName, string subscriptionName)
            : base(factory, connectionString, topicName, subscriptionName)
        {
        }
    }
}