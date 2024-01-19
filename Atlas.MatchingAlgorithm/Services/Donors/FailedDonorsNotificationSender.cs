using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Config;
using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IFailedDonorsNotificationSender
    {
        Task SendFailedDonorsAlert(
            IEnumerable<FailedDonorInfo> failedDonors,
            string alertSummary,
            Priority loggerPriority);
    }

    public class FailedDonorsNotificationSender : IFailedDonorsNotificationSender
    {
        private readonly INotificationSender notificationSender;

        public FailedDonorsNotificationSender(INotificationSender notificationSender)
        {
            this.notificationSender = notificationSender;
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
            
            await notificationSender.SendAlert(alertSummary, GetDescription(failedDonors), loggerPriority, NotificationConstants.OriginatorName);
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
            var count = failedDonors.Select(d => d.AtlasDonorId).Distinct().Count();

            return $"Failed donor count: {count}";
        }
    }
}
