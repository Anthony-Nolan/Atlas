using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Config;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications
{
    /// <summary>
    /// Sends data refresh messages to the appropriate support topic in order to notify or alert the support team.
    /// </summary>
    public interface IDataRefreshSupportNotificationSender
    {
        Task SendInitialisationNotification(int recordId);
        Task SendContinuationNotification(int recordId);
        Task SendSuccessNotification(int recordId);
        Task SendFailureAlert(int recordId);
        Task SendTeardownFailureAlert(int recordId);
        Task SendRequestManualTeardownNotification();
    }
    
    public class DataRefreshSupportNotificationSender: IDataRefreshSupportNotificationSender
    {
        private readonly INotificationSender notificationSender;

        public DataRefreshSupportNotificationSender(INotificationSender notificationSender)
        {
            this.notificationSender = notificationSender;
        }

        private async Task SendNotification(string summary, string description)
        {
            await notificationSender.SendNotification(summary, description, NotificationConstants.OriginatorName);
        }

        private async Task SendAlert(string summary, string description)
        {
            await notificationSender.SendAlert(summary, description, Priority.High, NotificationConstants.OriginatorName);
        }

        public async Task SendInitialisationNotification(int recordId)
        {
            var summary = $"Data refresh begun (#{recordId})";
            var description =
$@"The job to refresh all donor and hla data in the matching algorithm has begun (Record Id: {recordId}).
This is expected to happen once every three months, and to take a large number of hours to run to completion.
If no success or failure notification has been received within 24 hours of this one - check whether the job is still running.
If it is not, follow the instructions in the Readme of the search algorithm project.
Most urgently; scaling back the database the job was running on, as it is an expensive tier and should not be used when the job is not in progress";

            await SendNotification(summary, description);
        }

        /// <inheritdoc />
        public async Task SendContinuationNotification(int recordId)
        {
            var summary = $"Data refresh resumed (#{recordId})";
            var description =
$@"The matching algorithm data refresh (Record Id: {recordId}) has been resumed.
This should only be able to be manually triggered - and should have only happened if the single in-progress
refresh had been interrupted without success or failure.";

            await SendNotification(summary, description);
        }

        public async Task SendSuccessNotification(int recordId)
        {
            var summary = $"Data refresh successful (#{recordId})";
            var description = $@"The search algorithm data refresh (Record Id: {recordId}) was successful. Metrics will have been logged in application insights.";

            await SendNotification(summary, description);
        }

        public async Task SendFailureAlert(int recordId)
        {
            var summary = $"Data refresh failed (#{recordId})";
            var description =
$@"The matching algorithm data refresh (Record Id: {recordId}) has failed.
Appropriate teardown should have been run by the job itself.
Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description);
        }

        public async Task SendTeardownFailureAlert(int recordId)
        {
            var summary = $"Data refresh teardown failed (#{recordId})";
            var description =
$@"The teardown for matching algorithm data refresh (Record Id: {recordId}) has failed.
The (expensive) database has likely not been scaled down - this should be manually triggered as a matter of urgency.
Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description);
        }

        public async Task SendRequestManualTeardownNotification()
        {
            const string summary = "DATA REFRESH: Manual Teardown requested. This indicates that the data refresh failed unexpectedly.";
            const string description =
@"A manual teardown was requested, and the matching  algorithm has detected ongoing data-refresh jobs.
Appropriate teardown is being run. The data refresh will need to be re-started once the reason for the server restart has been diagnosed and handled.";

            await SendNotification(summary, description);
        }
    }
}