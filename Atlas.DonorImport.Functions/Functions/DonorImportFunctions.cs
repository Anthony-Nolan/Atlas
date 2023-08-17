using System.IO;
using System.Threading.Tasks;
using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Microsoft.Azure.WebJobs;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImportFunctions
    {
        private readonly IDonorFileImporter donorFileImporter;
        private readonly IDonorImportFileHistoryService donorHistoryService;
        private readonly IDonorImportMessageSender donorImportMessageSender;

        public DonorImportFunctions(IDonorFileImporter donorFileImporter, IDonorImportFileHistoryService donorHistoryService, IDonorImportMessageSender donorImportMessageSender)
        {
            this.donorFileImporter = donorFileImporter;
            this.donorHistoryService = donorHistoryService;
            this.donorImportMessageSender = donorImportMessageSender;
        }

        [FunctionName(nameof(ImportDonorFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task ImportDonorFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:ImportFileTopic%",
                "%MessagingServiceBus:ImportFileSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent, string messageId,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorFileImporter.ImportDonorFile(new DonorImportFile
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject,
                UploadTime = blobCreatedEvent.EventTime.UtcDateTime,
                MessageId = messageId
            });
        }

        [FunctionName(nameof(CheckForStalledImport))]
        public async Task CheckForStalledImport([TimerTrigger("%DonorImport:FileCheckCronSchedule%")] TimerInfo timer)
        {
            await donorHistoryService.SendNotificationForStalledImports();
        }

        [FunctionName(nameof(ImportDonorFileDeadLetterQueueListener))]
        public async Task ImportDonorFileDeadLetterQueueListener(
            [ServiceBusTrigger(
                "%MessagingServiceBus:ImportFileTopic%/Subscriptions/%MessagingServiceBus:ImportFileSubscription%/$DeadLetterQueue",
                "%MessagingServiceBus:ImportFileSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            EventGridSchema blobCreatedEvent)
        {
            await donorImportMessageSender.SendFailureMessage(blobCreatedEvent.Subject, ImportFaulireReason.RequestDeadlettered, string.Empty);
        }
    }
}