using System;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Atlas.Common.Notifications
{
    public interface INotificationsClient
    {
        Task SendAlert(Alert alert);
        Task SendNotification(Notification notification);
    }

    internal sealed class NotificationsClient : INotificationsClient, IAsyncDisposable
    {
        private readonly ITopicClient notificationTopicClient;
        private readonly ITopicClient alertTopicClient;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public NotificationsClient(NotificationsServiceBusSettings settings, [FromKeyedServices(typeof(NotificationsServiceBusSettings))]ITopicClientFactory topicClientFactory, ILogger logger)
        {
            notificationTopicClient = topicClientFactory.BuildTopicClient(settings.NotificationsTopic);
            alertTopicClient = topicClientFactory.BuildTopicClient(settings.AlertsTopic);
            sendRetryCount = settings.SendRetryCount;
            sendRetryCooldownSeconds = settings.SendRetryCooldownSeconds;
            this.logger = logger;
        }

        public async Task SendAlert(Alert alert)
        {
            var message = BuildMessage(alert);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await alertTopicClient.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send alert message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }

        public async Task SendNotification(Notification notification)
        {
            var message = BuildMessage(notification);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await notificationTopicClient.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send notification message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }

        private static ServiceBusMessage BuildMessage(BaseNotificationsMessage message)
        {
            var messageJson = JsonConvert.SerializeObject(message);
            var brokeredMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageJson));
            return brokeredMessage;
        }

        public async ValueTask DisposeAsync()
        {
            await notificationTopicClient.DisposeAsync();
            await alertTopicClient.DisposeAsync();
        }
    }
}