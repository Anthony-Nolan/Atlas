using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services
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
        private readonly IDonorService donorService;

        public DonorManagementService(IDonorService donorService)
        {
            this.donorService = donorService;
        }

        public async Task ManageDonorBatchByAvailability(IEnumerable<DonorAvailabilityUpdate> donorAvailabilityUpdates)
        {
            var updates = GetLatestUpdatePerDonorInBatch(donorAvailabilityUpdates).ToList();
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

            if (unavailableDonorIds.Any())
            {
                await donorService.DeleteDonorBatch(unavailableDonorIds);
            }
        }
    }
}
