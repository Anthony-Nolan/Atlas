using Atlas.MatchingAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IFailedDonorsNotificationSender
    {
        Task SendFailedDonorsAlert(
            IEnumerable<FailedDonorInfo> failedDonors,
            string alertSummary,
            Priority loggerPriority);
    }

    public class FailedDonorsNotificationSender : NotificationSender, IFailedDonorsNotificationSender
    {
        public FailedDonorsNotificationSender(
            INotificationsClient notificationsClient,
            ILogger logger) : base(notificationsClient, logger)
        {
        }

        public async Task SendFailedDonorsAlert(
            IEnumerable<FailedDonorInfo> failedDonors,
            string alertSummary,
            Priority loggerPriority)
        {
            failedDonors = failedDonors.ToList();

            if (!failedDonors.Any())
            {
                return;
            }
            
            await SendAlert(alertSummary, GetDescription(failedDonors), loggerPriority);
        }

        private static string GetDescription(IEnumerable<FailedDonorInfo> failedDonors)
        {
            failedDonors = failedDonors.ToList();

            var donorCount = GetDistinctDonorCountText(failedDonors);

            return $"{donorCount}.{Environment.NewLine}"
                   + $"Donor counts are for guidance only, and may be affected by missing donor info.{Environment.NewLine}"
                   + "An event has been logged for each failed donor in Application Insights.";
        }

        private static string GetDistinctDonorCountText(IEnumerable<FailedDonorInfo> failedDonors)
        {
            var count = failedDonors.Select(d => d.DonorId).Distinct().Count();

            return $"Failed donor count: {count}";
        }
    }
}
