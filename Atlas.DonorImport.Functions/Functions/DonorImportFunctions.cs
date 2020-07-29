using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImportFunctions
    {
        private readonly IDonorFileImporter donorFileImporter;

        public DonorImportFunctions(IDonorFileImporter donorFileImporter)
        {
            this.donorFileImporter = donorFileImporter;
        }

        /// IMPORTANT: Do not rename this function without careful consideration. This function is called by event grid, which has the function name set by terraform.
        // If changing this, you must also change the value hardcoded in the event_grid.tf terraform file. 
        [FunctionName(nameof(ImportDonorFile))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task ImportDonorFile(
            [EventGridTrigger] EventGridEvent blobCreatedEvent,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream // Raw JSON Text file containing donor updates in expected schema
        )
        {
            await donorFileImporter.ImportDonorFile(new DonorImportFile {Contents = blobStream, FileLocation = blobCreatedEvent.Subject, UploadTime = blobCreatedEvent.EventTime});
        }
    }
}