using Nova.Utils.Notifications;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Config;

namespace Nova.SearchAlgorithm.Services
{
    public abstract class NotificationSender
    {
        private readonly INotificationsClient notificationsClient;

        protected NotificationSender(INotificationsClient notificationsClient)
        {
            this.notificationsClient = notificationsClient;
        }

        protected async Task SendNotification(string summary, string description)
        {
            var notification = new Notification(summary, description, NotificationConstants.OriginatorName);

            await notificationsClient.SendNotification(notification);
        }

        protected async Task SendAlert(string summary, string description, Priority priority)
        {
            var alert = new Alert(summary, description, priority, NotificationConstants.OriginatorName);

            await notificationsClient.SendAlert(alert);
        }
    }
}