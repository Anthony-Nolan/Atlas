using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications.MessageModels;
using System;
using System.Threading.Tasks;

namespace Atlas.Common.Notifications
{
    public interface INotificationSender
    {
        /// <summary>
        /// Sends a Notification to the configured MessageBus.
        /// Will not leak any exceptions, if that can't be achieved
        /// </summary>
        Task SendNotification(string summary, string description, string source);

        /// <summary>
        /// Sends a Notification to the configured MessageBus.
        /// Will not leak any exceptions, if that can't be achieved
        /// </summary>
        Task SendAlert(string summary, string description, Priority priority, string source);
    }

    internal class NotificationSender : INotificationSender
    {
        private readonly INotificationsClient notificationsClient;
        private readonly ILogger logger;

        public NotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger)
        {
            this.notificationsClient = notificationsClient;
            this.logger = logger;
        }

        public async Task SendNotification(string summary, string description, string source)
        {
            description ??= "";
            var notification = new Notification(summary, description, source);

            try
            {
                logger.SendTrace($"{nameof(Notification)} sent from {notification.Originator}. Summary: {notification.Summary}. Detail: {notification.Description}");
                await notificationsClient.SendNotification(notification);
            }
            catch (Exception ex)
            {
                SendNotificationSenderFailureEvent(ex, notification);
            }
        }

        public async Task SendAlert(string summary, string description, Priority priority, string source)
        {
            description ??= "";
            var alert = new Alert(summary, description, priority, source);

            try
            {
                logger.SendTrace($"{nameof(Alert)} sent from {alert.Originator}. Priority: {alert.Priority.ToString()}. Summary: {alert.Summary}. Detail: {alert.Description}", LogLevel.Warn);
                await notificationsClient.SendAlert(alert);
            }
            catch (Exception ex)
            {
                SendNotificationSenderFailureEvent(ex, alert);
            }
        }

        private void SendNotificationSenderFailureEvent(Exception exception, BaseNotificationsMessage message)
        {
            try
            {
                logger.SendEvent(new NotificationSenderFailureEventModel(exception, message));
            }
            catch
            {
                // If we can't log, then there's not much point in whinging about it.
                // On the off chance that the underlying operation succeeded, we don't
                // want to crash a happy execution just because the logging broke.
                // So just swallow this.
            }
        }
    }
}