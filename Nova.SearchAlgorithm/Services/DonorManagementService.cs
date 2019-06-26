using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Clients;
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
        private readonly IDonorServiceClient donorServiceClient;
        private readonly IDonorService donorService;

        public DonorManagementService(
            IDonorServiceClient donorServiceClient,
            IDonorService donorService
        )
        {
            this.donorServiceClient = donorServiceClient;
            this.donorService = donorService;
        }

        public async Task ManageDonorByAvailability(DonorAvailabilityUpdate donorAvailabilityUpdate)
        {
            if (donorAvailabilityUpdate.IsAvailableForSearch)
            {
                await AddDonor(donorAvailabilityUpdate.DonorId);
            }
            else
            {
                await RemoveDonor(donorAvailabilityUpdate.DonorId);
            }
        }

        private async Task AddDonor(int donorId)
        {
            var donorInfo = await donorServiceClient.GetDonorInfoForSearchAlgorithm(donorId);
            await donorService.CreateOrUpdateDonorBatch(new[] {donorInfo.ToInputDonor()});
        }

        private async Task RemoveDonor(int donorId)
        {
            await donorService.DeleteDonor(donorId);
        }
    }
}
