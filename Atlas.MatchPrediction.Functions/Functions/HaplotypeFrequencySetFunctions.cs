using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IFrequencySetService frequencySetService;

        public HaplotypeFrequencySetFunctions(IFrequencySetService frequencySetService)
        {
            this.frequencySetService = frequencySetService;
        }

        [FunctionName(nameof(ImportHaplotypeFrequencySet))]
        public async Task ImportHaplotypeFrequencySet(
            [BlobTrigger("%AzureStorage:HaplotypeFrequencySetImportContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")]
            Stream blob,
            string fileName,
            BlobProperties properties)
        {
            using var file = new FrequencySetFile
            {
                Contents = blob,
                FileName = fileName,
                UploadedDateTime = properties.LastModified
            };

            await frequencySetService.ImportFrequencySet(file);
        }
    }
}