using Atlas.Common.AzureEventGrid;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Services.DonorIdChecker;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorIdCheckerFunctions
    {
        private readonly IDonorRecordIdChecker donorIdChecker;

        public DonorIdCheckerFunctions(IDonorRecordIdChecker donorIdChecker)
        {
            this.donorIdChecker = donorIdChecker;
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
            await donorIdChecker.CheckDonorIdsFromFile(new BlobImportFile
            {
                Contents = blobStream,
                FileLocation = blobCreatedEvent.Subject
            });
        }
    }
}
