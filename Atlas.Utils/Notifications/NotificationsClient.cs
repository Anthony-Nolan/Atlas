using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;

namespace Atlas.Utils.Notifications
{
    public interface INotificationsClient
    {
        Task SendAlert(Alert alert);
        Task SendNotification(Notification notification);
    }

    public class NotificationsClient : INotificationsClient
    {
        private readonly string connectionString;
        private readonly TopicClient notificationTopicClient;
        private readonly TopicClient alertTopicClient;

        public NotificationsClient(string connectionString, string notificationTopic, string alertTopic)
        {
            this.connectionString = connectionString;
            notificationTopicClient = CreateNewTopicClient(notificationTopic);
            alertTopicClient = CreateNewTopicClient(alertTopic);
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

        private Message BuildMessage(BaseNotificationsMessage message)
        {
            var messageJson = JsonConvert.SerializeObject(message);
            var brokeredMessage = new Message(Encoding.UTF8.GetBytes(messageJson));
            return brokeredMessage;
        }


        private TopicClient CreateNewTopicClient(string path)
        {
            var client = new TopicClient(connectionString, path);
            return client;
        }
    }
}