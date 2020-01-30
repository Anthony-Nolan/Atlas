using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshNotificationSender
    {
        Task SendInitialisationNotification();
        Task SendSuccessNotification();
        Task SendFailureAlert();
        Task SendTeardownFailureAlert();
    }
    
    public class DataRefreshNotificationSender: NotificationSender, IDataRefreshNotificationSender
    {
        public DataRefreshNotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger) : base(notificationsClient, logger)
        {
        }
        
        public async Task SendInitialisationNotification()
        {
            const string summary = "Data refresh begun";
            const string description = "The job to refresh all donor and hla data in the search algorithm has begun. " +
                                       "This is expected to happen once every three months, and to take a large number of hours to run to completion." +
                                       "If no success or failure notification has been received within 24 hours of this one - check whether the job is still running." +
                                       "If it is not, follow the instructions in the Readme of the search algorithm project." +
                                       "Most urgently; scaling back the database the job was running on, as it is an expensive tier and should not be used when the job is not in progress";

            await SendNotification(summary, description);
        }

        public async Task SendSuccessNotification()
        {
            const string summary = "Data refresh successful";
            const string description = "The search algorithm data refresh was successful. Metrics will have been logged in application insights.";

            await SendNotification(summary, description);
        }

        public async Task SendFailureAlert()
        {
            const string summary = "Data refresh failed";
            const string description = "The search algorithm data refresh has failed." +
                                       "Appropriate teardown should have been run by the job itself." +
                                       "Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description, Priority.High);
        }

        public async Task SendTeardownFailureAlert()
        {
            const string summary = "Data refresh teardown failed";
            const string description = "The search algorithm data refresh teardown has failed." +
                                       "The (expensive) database has likely not been scaled down - this should be manually triggered as a matter of urgency." +
                                       "Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description, Priority.High);
        }
    }
}