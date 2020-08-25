using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.Notifications.MessageModels;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Common.Notifications
{
    public interface INotificationsClient
    {
        Task SendAlert(Alert alert);
        Task SendNotification(Notification notification);
    }

    internal class NotificationsClient : INotificationsClient
    {
        private readonly ITopicClient notificationTopicClient;
        private readonly ITopicClient alertTopicClient;

        public NotificationsClient(NotificationsServiceBusSettings settings, ITopicClientFactory topicClientFactory)
        {
            notificationTopicClient = topicClientFactory.BuildTopicClient(settings.ConnectionString, settings.NotificationsTopic);
            alertTopicClient = topicClientFactory.BuildTopicClient(settings.ConnectionString, settings.AlertsTopic);
        }

        public async Task SendAlert(Alert alert)
        {
            var message = BuildMessage(alert);
            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await alertTopicClient.SendAsync(message);
            }
        }

        public async Task SendNotification(Notification notification)
        {
            var message = BuildMessage(notification);
            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await notificationTopicClient.SendAsync(message);
            }
        }

        private static Message BuildMessage(BaseNotificationsMessage message)
        {
            var messageJson = JsonConvert.SerializeObject(message);
            var brokeredMessage = new Message(Encoding.UTF8.GetBytes(messageJson));
            return brokeredMessage;
        }
    }
}