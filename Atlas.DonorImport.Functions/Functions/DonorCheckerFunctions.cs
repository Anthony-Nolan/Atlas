using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorChecker;
using Microsoft.Azure.Functions.Worker;

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

        [Function(nameof(CheckDonorIdsFromFile))]
        public async Task CheckDonorIdsFromFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorIdCheckerTopic%",
                "%MessagingServiceBus:DonorIdCheckerSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent,
            [BlobInput("{data.url}", Connection = "AzureStorage:ConnectionString")] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorIdChecker.CheckDonorIdsFromFile(new DonorIdCheckFile
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject
            });
        }

        [Function(nameof(CheckDonorInfoFromFile))]
        public async Task CheckDonorInfoFromFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorInfoCheckerTopic%",
                "%MessagingServiceBus:DonorInfoCheckerSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent,
            [BlobInput("{data.url}", Connection = "AzureStorage:ConnectionString")] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
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
