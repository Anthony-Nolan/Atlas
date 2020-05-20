using System.IO;
using Microsoft.Azure.WebJobs;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImport
    {
        [FunctionName(nameof(ImportDonorFile))]
        public void ImportDonorFile(
            [BlobTrigger("%AzureStorage:DonorFileBlobContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")]
            Stream blobStream,
            string fileName)
        {
        }
    }
}