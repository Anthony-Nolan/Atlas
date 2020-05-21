using System.IO;
using System.Threading.Tasks;
using Atlas.DonorImport.Services;
using Microsoft.Azure.WebJobs;

namespace Atlas.DonorImport.Functions.Functions
{
    public class DonorImport
    {
        private readonly IDonorFileImporter donorFileImporter;

        public DonorImport(IDonorFileImporter donorFileImporter)
        {
            this.donorFileImporter = donorFileImporter;
        }
        
        [FunctionName(nameof(ImportDonorFile))]
        public async Task ImportDonorFile(
            [BlobTrigger("%AzureStorage:DonorFileBlobContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")]
            Stream blobStream,
            string fileName)
        {
            await donorFileImporter.ImportDonorFile(blobStream, fileName);
        }
    }
}