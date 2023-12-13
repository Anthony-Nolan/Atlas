using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using System.Web.Http;
using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorUpdates;
using Azure.Core;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.OData;
using Newtonsoft.Json;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImportFunctions
    {
        private readonly IDonorFileImporter donorFileImporter;
        private readonly IDonorImportFileHistoryService donorHistoryService;
        private readonly IDonorImportMessageSender donorImportMessageSender;
        private readonly IDonorImportFailuresCleaner donorImportFailuresCleaner;
        private readonly IDonorUpdatesSaver donorUpdatesSaver;

        public DonorImportFunctions(
            IDonorFileImporter donorFileImporter,
            IDonorImportFileHistoryService donorHistoryService,
            IDonorImportMessageSender donorImportMessageSender,
            IDonorImportFailuresCleaner donorImportFailuresCleaner,
            IDonorUpdatesSaver donorUpdatesSaver)
        {
            this.donorFileImporter = donorFileImporter;
            this.donorHistoryService = donorHistoryService;
            this.donorImportMessageSender = donorImportMessageSender;
            this.donorImportFailuresCleaner = donorImportFailuresCleaner;
            this.donorUpdatesSaver = donorUpdatesSaver;
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

        [FunctionName(nameof(ManuallyPublishDonorUpdatesByDonorId))]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ManuallyPublishDonorUpdatesByDonorId([HttpTrigger(AuthorizationLevel.Function, "post")][RequestBodyType(typeof(int[]), "Donor ids")]HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var ids = JsonConvert.DeserializeObject<int[]>(await reader.ReadToEndAsync());

            await donorUpdatesSaver.GenerateAndSave(ids);
            return new OkResult();
        }
    }
}