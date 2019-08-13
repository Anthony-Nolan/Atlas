using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    /// <summary>
    /// Manages the addition or removal of individual donors from the search algorithm database.
    /// </summary>
    public interface IDonorManagementService
    {
        Task ManageDonorBatchByAvailability(IEnumerable<DonorAvailabilityUpdate> donorAvailabilityUpdates);
    }

    public class DonorManagementService : IDonorManagementService
    {
        private const string TraceMessagePrefix = nameof(ManageDonorBatchByAvailability);

        private readonly IDonorService donorService;
        private readonly ILogger logger;

        public DonorManagementService(IDonorService donorService, ILogger logger)
        {
            this.donorService = donorService;
            this.logger = logger;
        }

        public async Task ManageDonorBatchByAvailability(IEnumerable<DonorAvailabilityUpdate> donorAvailabilityUpdates)
        {
            var updates = GetLatestUpdatePerDonorInBatch(donorAvailabilityUpdates).ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {updates.Count} donor updates to be applied.", LogLevel.Info);

            await AddOrUpdateDonors(updates);
            await RemoveDonors(updates);
        }

        private static IEnumerable<DonorAvailabilityUpdate> GetLatestUpdatePerDonorInBatch(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            return updates
                .GroupBy(u => u.DonorId)
                .Select(grp => grp.OrderByDescending(u => u.UpdateSequenceNumber).First());
        }

        private async Task AddOrUpdateDonors(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var availableDonors = updates
                .Where(update => update.IsAvailableForSearch)
                .Select(d => d.DonorInfo)
                .ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {availableDonors.Count} donors to be added or updated.", LogLevel.Info);

            if (availableDonors.Any())
            {
                await donorService.CreateOrUpdateDonorBatch(availableDonors);
            }
        }

        private async Task RemoveDonors(IEnumerable<DonorAvailabilityUpdate> updates)
        {
            var unavailableDonorIds = updates
                .Where(update => !update.IsAvailableForSearch)
                .Select(d => d.DonorId)
                .ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {unavailableDonorIds.Count} donors to be removed.", LogLevel.Info);

            if (unavailableDonorIds.Any())
            {
                await donorService.DeleteDonorBatch(unavailableDonorIds);
            }
        }
    }
}
