using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
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
        private const string AlertSummary = "Searchable Donor Update Conversion Failure(s) in Search Algorithm";

        private readonly IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService;
        private readonly IDonorManagementService donorManagementService;
        private readonly ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private readonly IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private readonly ILogger logger;
        private readonly int batchSize;

        public DonorUpdateProcessor(
            IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService,
            IDonorManagementService donorManagementService,
            ISearchableDonorUpdateConverter searchableDonorUpdateConverter,
            IFailedDonorsNotificationSender failedDonorsNotificationSender,
            ILogger logger,
            int batchSize)
        {
            this.messageProcessorService = messageProcessorService;
            this.donorManagementService = donorManagementService;
            this.searchableDonorUpdateConverter = searchableDonorUpdateConverter;
            this.failedDonorsNotificationSender = failedDonorsNotificationSender;
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
            var converterResults = await searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(messageBatch);

            logger.SendTrace($"{TraceMessagePrefix}: {converterResults.ProcessingResults.Count()} messages retrieved for processing.", LogLevel.Info);

            if (converterResults.ProcessingResults.Any())
            {
                await donorManagementService.ManageDonorBatchByAvailability(converterResults.ProcessingResults);
            }

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                converterResults.FailedDonors,
                AlertSummary,
                Priority.Medium);
        }
    }
}
