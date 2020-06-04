using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
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
        public async Task ImportDonorFile(
            // Raw JSON Text file containing donor updates in expected schema
            [BlobTrigger("%AzureStorage:DonorFileBlobContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")]
            Stream blobStream,
            string fileName)
        {
            await donorFileImporter.ImportDonorFile(new DonorImportFile {Contents = blobStream, FileName = fileName});
        }
    }
}