using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services
{
    public abstract class NotificationSender
    {
        private readonly INotificationsClient notificationsClient;
        private readonly ILogger logger;

        protected NotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger)
        {
            this.notificationsClient = notificationsClient;
            this.logger = logger;
        }

        protected async Task SendNotification(string summary, string description)
        {
            var notification = new Notification(summary, description, NotificationConstants.OriginatorName);

            try
            {
                await notificationsClient.SendNotification(notification);
            }
            catch (Exception ex)
            {
                SendNotificationSenderFailureEvent(ex, notification);
            }
        }

        protected async Task SendAlert(string summary, string description, Priority priority)
        {
            var alert = new Alert(summary, description, priority, NotificationConstants.OriginatorName);

            try
            {
                await notificationsClient.SendAlert(alert);
            }
            catch (Exception ex)
            {
                SendNotificationSenderFailureEvent(ex, alert);
            }
        }

        private void SendNotificationSenderFailureEvent(Exception exception, BaseNotificationsMessage message)
        {
            logger.SendEvent(new NotificationSenderFailureEventModel(exception, message));
        }
    }
}