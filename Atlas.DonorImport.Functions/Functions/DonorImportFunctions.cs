using System.IO;
using Atlas.DonorImport.Services;
using Microsoft.Azure.WebJobs;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImportFunctions
    {
        private readonly IDonorImporter donorImporter;

        public DonorImportFunctions(IDonorImporter donorImporter)
        {
            this.donorImporter = donorImporter;
        }
        
        [FunctionName(nameof(ImportDonorFile))]
        public void ImportDonorFile(
            [BlobTrigger("%AzureStorage:DonorFileBlobContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")]
            Stream blobStream,
            string fileName)
        {
            donorImporter.ImportDonorFile(blobStream);
        }
    }
}