using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImportFunctions
    {
        private readonly IDonorFileImporter donorFileImporter;

        public DonorImportFunctions(IDonorFileImporter donorFileImporter)
        {
            this.donorFileImporter = donorFileImporter;
        }

        [FunctionName(nameof(ImportDonorFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        [Singleton]
        public async Task ImportDonorFile(
            [ServiceBusTrigger(
                "%MessagingServiceBus:ImportFileTopic%",
                "%MessagingServiceBus:ImportFileSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridEvent blobCreatedEvent,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorFileImporter.ImportDonorFile(new DonorImportFile {Contents = blobStream, FileLocation = blobCreatedEvent.Subject, UploadTime = blobCreatedEvent.EventTime});
        }
    }
}