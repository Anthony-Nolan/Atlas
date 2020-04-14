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
        private const string UnknownRegistryCodeText = "[Unknown]";

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

            var donorCountByRegistry = GetDonorCountByRegistryString(failedDonors);

            return $"{donorCountByRegistry}.{Environment.NewLine}"
                   + $"Donor counts are for guidance only, and may be affected by missing donor info.{Environment.NewLine}"
                   + "An event has been logged for each failed donor in Application Insights.";
        }

        private static string GetDonorCountByRegistryString(IEnumerable<FailedDonorInfo> failedDonors)
        {
            var counts = failedDonors
                .GroupBy(d => new { d.DonorId, d.RegistryCode })
                .GroupBy(d => GetValueOrDefault(d.Key.RegistryCode, UnknownRegistryCodeText))
                .OrderBy(grp => grp.Key)
                .Select(grp => $"{grp.Key} - {grp.Count()}");

            return $"Failed donor count by registry: {string.Join(", ", counts)}";
        }

        private static string GetValueOrDefault(string value, string defaultIfEmpty)
            => string.IsNullOrEmpty(value) ? defaultIfEmpty : value;
    }
}
