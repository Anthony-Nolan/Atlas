using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;
using System.Collections.Generic;
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
        private readonly IMessageProcessorService<SearchableDonorUpdateModel> messageProcessorService;
        private readonly IDonorManagementService donorManagementService;
        private readonly int batchSize;

        public DonorUpdateProcessor(
            IMessageProcessorService<SearchableDonorUpdateModel> messageProcessorService,
            IDonorManagementService donorManagementService,
            int batchSize)
        {
            this.messageProcessorService = messageProcessorService;
            this.donorManagementService = donorManagementService;
            this.batchSize = batchSize;
        }

        public async Task ProcessDonorUpdates()
        {
            await messageProcessorService.ProcessMessageBatch(batchSize, async batch =>
            {
                await ProcessMessages(batch);
            });
        }

        private async Task ProcessMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> messageBatch)
        {
            var updates = messageBatch.Select(MapDonorAvailabilityUpdate);
            await donorManagementService.ManageDonorBatchByAvailability(updates);
        }

        /// <summary>
        /// Map directly rather than using AutoMapper to improve performance
        /// </summary>
        private static DonorAvailabilityUpdate MapDonorAvailabilityUpdate(ServiceBusMessage<SearchableDonorUpdateModel> update)
        {
            if (int.TryParse(update.DeserializedBody.DonorId, out var donorId))
            {
                var donorAvailabilityUpdate = new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = update.SequenceNumber,
                    DonorId = donorId,
                    DonorInfo = update.DeserializedBody.SearchableDonorInformation?.ToInputDonor(),
                    IsAvailableForSearch = update.DeserializedBody.IsAvailableForSearch
                };
                return donorAvailabilityUpdate;
            };

            throw new DonorImportException($"Could not parse donor id: {update.DeserializedBody.DonorId} to an int"); ;
        }
    }
}
