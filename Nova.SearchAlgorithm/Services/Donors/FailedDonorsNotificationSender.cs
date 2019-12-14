using Nova.SearchAlgorithm.Models;
using Nova.Utils.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Donors
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
        private const string UnknownDonorIdText = "[Donor(s) without ID]";
        private const string UnknownRegistryCodeText = "[Unknown]";
        private const int MaxDonorIdCount = 25;

        public FailedDonorsNotificationSender(INotificationsClient notificationsClient) : base(notificationsClient)
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

            var donorIds = GetDonorIdsString(failedDonors);
            var donorCountByRegistry = GetDonorCountByRegistryString(failedDonors);

            return $"{donorIds}.{Environment.NewLine}"
                   + $"{donorCountByRegistry}.{Environment.NewLine}"
                   + $"Note: The above donor counts may differ, especially if some donors are missing info.{Environment.NewLine}"
                   + "An event has been logged for each failed donor in Application Insights.";
        }

        private static string GetDonorIdsString(IEnumerable<FailedDonorInfo> failedDonors)
        {
            var donorIds = failedDonors
                .Select(d => GetValueOrDefault(d.DonorId, UnknownDonorIdText))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            return donorIds.Count <= MaxDonorIdCount
                ? $"Processing failed for donor(s): {string.Join(", ", donorIds)}"
                : $"{donorIds.Count} donors failed to be processed";
        }

        private static string GetDonorCountByRegistryString(IEnumerable<FailedDonorInfo> failedDonors)
        {
            var counts = failedDonors
                .GroupBy(d => new { d.DonorId, d.RegistryCode })
                .GroupBy(d => GetValueOrDefault(d.Key.RegistryCode, UnknownRegistryCodeText))
                .Select(grp => $"{grp.Key} - {grp.Count()}");

            return $"Donor count by registry: {string.Join(", ", counts)}";
        }

        private static string GetValueOrDefault(string value, string defaultIfEmpty)
            => string.IsNullOrEmpty(value) ? defaultIfEmpty : value;
    }
}
