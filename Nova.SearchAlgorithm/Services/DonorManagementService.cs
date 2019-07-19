using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Extensions;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services
{
    /// <summary>
    /// Manages the addition or removal of individual donors from the search algorithm database.
    /// </summary>
    public interface IDonorManagementService
    {
        Task ManageDonorByAvailability(DonorAvailabilityUpdate donorAvailabilityUpdate);
    }

    public class DonorManagementService : IDonorManagementService
    {
        private readonly IDonorService donorService;

        public DonorManagementService(IDonorService donorService)
        {
            this.donorService = donorService;
        }

        public async Task ManageDonorByAvailability(DonorAvailabilityUpdate donorAvailabilityUpdate)
        {
            if (donorAvailabilityUpdate.IsAvailableForSearch)
            {
                await AddDonor(donorAvailabilityUpdate);
            }
            else
            {
                await RemoveDonor(donorAvailabilityUpdate.DonorId);
            }
        }

        private async Task AddDonor(DonorAvailabilityUpdate donorAvailabilityUpdate)
        {
            await donorService.CreateOrUpdateDonorBatch(new[]
            {
                donorAvailabilityUpdate.DonorInfoForSearchAlgorithm.ToInputDonor()
            });
        }

        private async Task RemoveDonor(int donorId)
        {
            await donorService.DeleteDonor(donorId);
        }
    }
}
