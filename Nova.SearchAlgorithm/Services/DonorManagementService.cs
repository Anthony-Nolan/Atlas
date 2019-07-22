using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Http.Exceptions;
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
        private readonly ILogger logger;

        public DonorManagementService(
            IDonorService donorService,
            ILogger logger)
        {
            this.donorService = donorService;
            this.logger = logger;
        }

        public async Task ManageDonorByAvailability(DonorAvailabilityUpdate donorAvailabilityUpdate)
        {
            if (donorAvailabilityUpdate.IsAvailableForSearch)
            {
                await AddOrUpdateDonor(donorAvailabilityUpdate);
            }
            else
            {
                await RemoveDonor(donorAvailabilityUpdate.DonorId);
            }
        }

        private async Task AddOrUpdateDonor(DonorAvailabilityUpdate donorAvailabilityUpdate)
        {
            await donorService.CreateOrUpdateDonorBatch(new[]
            {
                donorAvailabilityUpdate.DonorInfo.ToInputDonor()
            });
        }

        private async Task RemoveDonor(int donorId)
        {
            try
            {
                await donorService.DeleteDonor(donorId);
            }
            catch (NovaNotFoundException exception)
            {
                logger.SendEvent(new DonorDeletionFailureEventModel(exception, donorId.ToString()));
            }
        }
    }
}
