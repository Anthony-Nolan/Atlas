using System.Threading.Tasks;
using Nova.Utils.Notifications;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface INotificationSender
    {
        Task SendInitialisationNotification();
        Task SendSuccessNotification();
        Task SendFailureAlert();
    }
    
    public class NotificationSender: INotificationSender
    {
        private readonly INotificationsClient notificationsClient;
        private const string Originator = "Nova.SearchAlgorithm";

        public NotificationSender(INotificationsClient notificationsClient)
        {
            this.notificationsClient = notificationsClient;
        }
        
        public async Task SendInitialisationNotification()
        {
            const string summary = "Data refresh begun";
            const string description = "The job to refresh all donor and hla data in the search algorithm has begun. " +
                                       "This is expected to happen once every three months, and to take a large number of hours to run to completion." +
                                       "If no success or failure notification has been received within 24 hours of this one - check whether the job is still running." +
                                       "If it is not, follow the instructions in the Readme of the search algorithm project." +
                                       "Most urgently; scaling back the database the job was running on, as it is an expensive tier and should not be used when the job is not in progress";
            var notification = new Notification(summary, description, Originator);

            await notificationsClient.SendNotification(notification);
        }

        public async Task SendSuccessNotification()
        {
            const string summary = "Data refresh successful";
            const string description = "The search algorithm data refresh was successful. Metrics will have been logged in application insights.";
            var notification = new Notification(summary, description, Originator);

            await notificationsClient.SendNotification(notification);
        }

        public async Task SendFailureAlert()
        {
            const string summary = "Data refresh failed";
            const string description = "The search algorithm data refresh has failed." +
                                       "Appropriate teardown should have been run by the job itself." +
                                       "Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";
            var alert = new Alert(summary, description, Priority.High, Originator);

            await notificationsClient.SendAlert(alert);
        }
    }
}