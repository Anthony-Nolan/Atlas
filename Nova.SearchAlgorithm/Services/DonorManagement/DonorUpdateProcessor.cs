using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ServiceBus.BatchReceiving;
using Nova.Utils.ServiceBus.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private readonly int batchSize;
        private readonly ILogger logger;

        public DonorUpdateProcessor(
            IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService,
            IDonorManagementService donorManagementService,
            ISearchableDonorUpdateConverter searchableDonorUpdateConverter,
            ILogger logger,
            int batchSize)
        {
            this.messageProcessorService = messageProcessorService;
            this.donorManagementService = donorManagementService;
            this.searchableDonorUpdateConverter = searchableDonorUpdateConverter;
            this.logger = logger;
            this.batchSize = batchSize;
        }

        public async Task ProcessDonorUpdates()
        {
            await messageProcessorService.ProcessMessageBatch(batchSize, async batch =>
            {
                await ProcessMessages(batch);
            }, prefetchCount: batchSize * 2);
        }

        private async Task ProcessMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>> messageBatch)
        {
            var updates = (await searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(messageBatch)).ToList();

            logger.SendTrace($"{TraceMessagePrefix}: {updates.Count} messages retrieved for processing.", LogLevel.Info);

            if (updates.Any())
            {
                await donorManagementService.ManageDonorBatchByAvailability(updates);
            }
        }
    }
}
