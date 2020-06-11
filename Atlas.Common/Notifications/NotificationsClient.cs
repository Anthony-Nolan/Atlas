using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Notifications.MessageModels;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Common.Notifications
{
    internal interface INotificationsClient
    {
        Task SendAlert(Alert alert);
        Task SendNotification(Notification notification);
    }

    internal class NotificationsClient : INotificationsClient
    {
        private readonly TopicClient notificationTopicClient;
        private readonly TopicClient alertTopicClient;

        public NotificationsClient(IOptions<NotificationsServiceBusSettings> settings)
        {
            notificationTopicClient = new TopicClient(settings.Value.ConnectionString, settings.Value.NotificationsTopic);
            alertTopicClient = new TopicClient(settings.Value.ConnectionString, settings.Value.AlertsTopic);
        }

        public async Task SendAlert(Alert alert)
        {
            var message = BuildMessage(alert);
            await alertTopicClient.SendAsync(message);
        }

        public async Task SendNotification(Notification notification)
        {
            var message = BuildMessage(notification);
            await notificationTopicClient.SendAsync(message);
        }

        private static Message BuildMessage(BaseNotificationsMessage message)
        {
            var messageJson = JsonConvert.SerializeObject(message);
            var brokeredMessage = new Message(Encoding.UTF8.GetBytes(messageJson));
            return brokeredMessage;
        }
    }
}