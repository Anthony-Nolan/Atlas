using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.Models;
using Nova.Utils.ServiceBus.BatchReceiving;
using Nova.Utils.ServiceBus.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.DonorManagement
{
    public interface IDonorUpdateProcessor
    {
        Task ProcessDonorUpdates();
    }

    public class DonorUpdateProcessor : IDonorUpdateProcessor
    {
        private const string TraceMessagePrefix = nameof(ProcessDonorUpdates);

        private readonly IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService;
        private readonly IDonorManagementService donorManagementService;
        private readonly int batchSize;
        private readonly ILogger logger;

        public DonorUpdateProcessor(
            IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService,
            IDonorManagementService donorManagementService,
            ILogger logger,
            int batchSize)
        {
            this.messageProcessorService = messageProcessorService;
            this.donorManagementService = donorManagementService;
            this.logger = logger;
            this.batchSize = batchSize;
        }

        public async Task ProcessDonorUpdates()
        {
            logger.SendTrace($"{TraceMessagePrefix}: Commenced processing of message batch.", LogLevel.Info);

            await messageProcessorService.ProcessMessageBatch(batchSize, async batch =>
            {
                await ProcessMessages(batch);
            });

            logger.SendTrace($"{TraceMessagePrefix}: Completed processing of message batch.", LogLevel.Info);
        }

        private async Task ProcessMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> messageBatch)
        {
            var updates = messageBatch.Select(MapDonorAvailabilityUpdate).ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {updates.Count} messages retrieved for processing.", LogLevel.Info);

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
