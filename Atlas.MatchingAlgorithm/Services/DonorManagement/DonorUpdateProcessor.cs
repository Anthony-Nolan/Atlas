using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Services.DonorManagement
{
    public interface IDonorUpdateProcessor
    {
        Task ProcessDonorUpdates();
    }

    public class DonorUpdateProcessor : IDonorUpdateProcessor
    {
        private const string TraceMessagePrefix = nameof(ProcessDonorUpdates);

        private readonly IMessageProcessor<SearchableDonorUpdate> messageProcessorService;
        private readonly IDonorManagementService donorManagementService;
        private readonly ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private readonly ILogger logger;
        private readonly int batchSize;

        public DonorUpdateProcessor(
            IMessageProcessor<SearchableDonorUpdate> messageProcessorService,
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

        private async Task ProcessMessages(IEnumerable<ServiceBusMessage<SearchableDonorUpdate>> messageBatch)
        {
            var converterResults = await searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(messageBatch);

            logger.SendTrace($"{TraceMessagePrefix}: {converterResults.ProcessingResults.Count()} messages retrieved for processing.", LogLevel.Info);

            if (converterResults.ProcessingResults.Any())
            {
                await donorManagementService.ManageDonorBatchByAvailability(converterResults.ProcessingResults);
            }
        }
    }
}
