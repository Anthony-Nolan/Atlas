using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchPrediction.Config;
using System;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IFailedImportNotificationSender
    {
        Task SendFailedImportAlert(string alertSummary, string fileName, Exception exception);
    }

    public class FailedImportNotificationSender : NotificationSender, IFailedImportNotificationSender
    {
        public FailedImportNotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger) : base(notificationsClient, logger, NotificationConstants.OriginatorName)
        {
        }

        public async Task SendFailedImportAlert(string alertSummary, string fileName, Exception exception)
        {
            await SendAlert(
                alertSummary, 
                $"Import of file, '{fileName}', failed with the following exception message: {exception.Message}. " +
                    "Full exception info has been logged to Application Insights.",
                Priority.High);
        }
    }
}