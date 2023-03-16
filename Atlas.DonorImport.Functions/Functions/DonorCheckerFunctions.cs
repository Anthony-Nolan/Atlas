using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorIdChecker;
using Atlas.DonorImport.Services.DonorChecker;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorCheckerFunctions
    {
        private readonly IDonorIdChecker donorIdChecker;
        private readonly IDonorInfoChecker donorInfoChecker;

        public DonorCheckerFunctions(IDonorIdChecker donorIdChecker, IDonorInfoChecker donorInfoChecker)
        {
            this.donorIdChecker = donorIdChecker;
            this.donorInfoChecker = donorInfoChecker;
        }

        [FunctionName(nameof(CheckDonorIdsFromFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task CheckDonorIdsFromFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorIdCheckerTopic%",
                "%MessagingServiceBus:DonorIdCheckerSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent, string messageId,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorIdChecker.CheckDonorIdsFromFile(new DonorIdCheckFile
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject
            });
        }

        [FunctionName(nameof(CompareDonorsFromFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task CompareDonorsFromFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorInfoCheckerTopic%",
                "%MessagingServiceBus:DonorInfoCheckerSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent, string messageId,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(new DonorImportFile()
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject
            });
        }
    }
}
