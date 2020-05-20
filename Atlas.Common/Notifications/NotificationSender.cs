using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications.MessageModels;
using System;
using System.Threading.Tasks;

namespace Atlas.Common.Notifications
{
    public abstract class NotificationSender
    {
        private readonly INotificationsClient notificationsClient;
        private readonly ILogger logger;
        private readonly string originatorName;

        protected NotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger,
            string originatorName)
        {
            this.notificationsClient = notificationsClient;
            this.logger = logger;
            this.originatorName = originatorName;
        }

        protected async Task SendNotification(string summary, string description)
        {
            var notification = new Notification(summary, description, originatorName);

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
            var alert = new Alert(summary, description, priority, originatorName);

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