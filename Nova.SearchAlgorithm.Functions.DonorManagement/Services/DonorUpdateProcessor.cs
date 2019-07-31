using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services
{
    public interface IDonorUpdateProcessor
    {
        Task ProcessDonorUpdates();
    }

    public class DonorUpdateProcessor : IDonorUpdateProcessor
    {
        private readonly IMessageReceiverService<SearchableDonorUpdateModel> messageReceiverService;
        private readonly IDonorManagementService donorManagementService;
        private readonly int batchSize;

        public DonorUpdateProcessor(
            IMessageReceiverService<SearchableDonorUpdateModel> messageReceiverService,
            IDonorManagementService donorManagementService,
            int batchSize)
        {
            this.messageReceiverService = messageReceiverService;
            this.donorManagementService = donorManagementService;
            this.batchSize = batchSize;
        }

        public async Task ProcessDonorUpdates()
        {
            var updates = await messageReceiverService.ReceiveMessageBatch(batchSize);
            var donorAvailabilityUpdates = updates.Select(MapDonorAvailabilityUpdate);
            await donorManagementService.ManageDonorBatchByAvailability(donorAvailabilityUpdates);
        }



        /// <summary>
        /// Map directly rather than using AutoMapper to improve performance
        /// </summary>
        private static DonorAvailabilityUpdate MapDonorAvailabilityUpdate(SearchableDonorUpdateModel update)
        {
            if (int.TryParse(update.DonorId, out var donorId))
            {
                var donorAvailabilityUpdate = new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = update.SearchableDonorInformation?.ToInputDonor(),
                    IsAvailableForSearch = update.IsAvailableForSearch
                };
                return donorAvailabilityUpdate;
            };

            throw new DonorImportException($"Could not parse donor id: {update.DonorId} to an int"); ;
        }
    }
}
