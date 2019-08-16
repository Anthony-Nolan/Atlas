using Nova.Utils.Notifications;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services
{
    public abstract class NotificationSender
    {
        private readonly INotificationsClient notificationsClient;
        private const string Originator = "Nova.SearchAlgorithm";

        protected NotificationSender(INotificationsClient notificationsClient)
        {
            this.notificationsClient = notificationsClient;
        }

        protected async Task SendNotification(string summary, string description)
        {
            var notification = new Notification(summary, description, Originator);

            await notificationsClient.SendNotification(notification);
        }

        protected async Task SendAlert(string summary, string description, Priority priority)
        {
            var alert = new Alert(summary, description, priority, Originator);

            await notificationsClient.SendAlert(alert);
        }
    }
}