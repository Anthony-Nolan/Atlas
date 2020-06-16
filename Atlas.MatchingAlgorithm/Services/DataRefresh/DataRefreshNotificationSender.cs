using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Config;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshNotificationSender
    {
        Task SendInitialisationNotification();
        Task SendContinuationNotification();
        Task SendSuccessNotification();
        Task SendFailureAlert();
        Task SendTeardownFailureAlert();
        Task SendRequestManualTeardownNotification();
        Task SendRecommendManualCleanupAlert();
    }
    
    public class DataRefreshNotificationSender: IDataRefreshNotificationSender
    {
        private readonly INotificationSender notificationSender;

        public DataRefreshNotificationSender(INotificationSender notificationSender)
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

        public async Task SendInitialisationNotification()
        {
            const string summary = "Data refresh begun";
            const string description = "The job to refresh all donor and hla data in the matching algorithm has begun. " +
                                       "This is expected to happen once every three months, and to take a large number of hours to run to completion." +
                                       "If no success or failure notification has been received within 24 hours of this one - check whether the job is still running." +
                                       "If it is not, follow the instructions in the Readme of the search algorithm project." +
                                       "Most urgently; scaling back the database the job was running on, as it is an expensive tier and should not be used when the job is not in progress";

            await SendNotification(summary, description);
        }

        /// <inheritdoc />
        public async Task SendContinuationNotification()
        {
            const string summary = "Data refresh resumed";
            const string description = "The matching algorithm data refresh has been resumed." +
                                       "This should only be able to be manually triggered - and should have only happened if the single in-progress " +
                                       "refresh had been interrupted without success or failure.";

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
            const string description = "The matching algorithm data refresh has failed." +
                                       "Appropriate teardown should have been run by the job itself." +
                                       "Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description);
        }

        public async Task SendTeardownFailureAlert()
        {
            const string summary = "Data refresh teardown failed";
            const string description = "The matching algorithm data refresh teardown has failed." +
                                       "The (expensive) database has likely not been scaled down - this should be manually triggered as a matter of urgency." +
                                       "Check application insights to track down the failure - the job may need to be restarted manually once issues have been resolved.";

            await SendAlert(summary, description);
        }

        public async Task SendRequestManualTeardownNotification()
        {
            const string summary =
                "DATA REFRESH: Manual Teardown requested. This indicates that the data refresh failed unexpectedly.";
            const string description =
            @"A manual teardown was requested, and the matching  algorithm has detected ongoing data-refresh jobs.
              Appropriate teardown is being run. The data refresh will need to be re-started once the reason for the server restart has been diagnosed and handled.";

            await SendNotification(summary, description);
        }

        public async Task SendRecommendManualCleanupAlert()
        {
            const string summary = "Data Refresh: Manual cleanup recommended.";
            const string description = 
            @"The algorithm has detected an in-progress data refresh job on startup. This generally implies that a data refresh job 
                    was terminated without the appropriate teardown being run - this should only happen if the service was re-started unexpectedly. 
                    Possible causes could include: (a) the service plan running out of memory (b) an azure outage (c) a deployment of the algorithm service. 
                    We should confirm that this was the case, and if so, run appropriate clean-up. See the README of the Atlas.MatchingAlgorithm project for more information. 
                    The function `RunDataRefreshCleanup` should encapsulate the majority of the necessary clean-up. 
                    CAVEAT: Due to restrictions of triggers in Azure functions, this function will run once a year not at start-up. 
                    Check the crontab of the `CheckIfCleanupNecessary` function to ensure it isn't this known false positive.'";;

            await SendAlert(summary, description);
        }
    }
}