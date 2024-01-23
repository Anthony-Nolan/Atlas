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
        private readonly IDonorImportFailuresCleaner donorImportFailuresCleaner;

        public DonorImportFunctions(
            IDonorFileImporter donorFileImporter,
            IDonorImportFileHistoryService donorHistoryService,
            IDonorImportMessageSender donorImportMessageSender,
            IDonorImportFailuresCleaner donorImportFailuresCleaner)
        {
            this.donorFileImporter = donorFileImporter;
            this.donorHistoryService = donorHistoryService;
            this.donorImportMessageSender = donorImportMessageSender;
            this.donorImportFailuresCleaner = donorImportFailuresCleaner;
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
            await donorImportMessageSender.SendFailureMessage(blobCreatedEvent.Subject, ImportFailureReason.RequestDeadlettered, string.Empty);
        }

        [FunctionName(nameof(DeleteDonorImportFailures))]
        public async Task DeleteDonorImportFailures([TimerTrigger("%FailureLogs:DeletionCronSchedule%")] TimerInfo timer)
        {
            await donorImportFailuresCleaner.DeleteExpiredDonorImportFailures();
        }
    }
}