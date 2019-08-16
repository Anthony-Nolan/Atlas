using System;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.Notifications;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    public interface IDonorManagementNotificationSender
    {
        Task SendDonorUpdatesNotAppliedNotification(IEnumerable<DonorAvailabilityUpdate> updates);
    }

    public class DonorManagementNotificationSender : NotificationSender, IDonorManagementNotificationSender
    {
        public DonorManagementNotificationSender(INotificationsClient notificationsClient) : base(notificationsClient)
        {
        }

        public async Task SendDonorUpdatesNotAppliedNotification(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            const string summary = "Donor Availability Updates Not Applied";
            const string message = "The following donor availability updates were not applied to " 
                                    + "the search algorithm database, as they were not newer than the last logged updates "
                                    + @"for the listed DonorIds.
                                    ";

            var updateInfo = updates
                .Select(u => $"DonorId: {u.DonorId}, IsAvailableForSearch: {u.IsAvailableForSearch}, UpdateSequenceNumber: {u.UpdateSequenceNumber}")
                .ToList();

            if (updateInfo.Any())
            {
                await SendNotification(summary, message + string.Join(Environment.NewLine, updateInfo));
            }
        }
    }
}
