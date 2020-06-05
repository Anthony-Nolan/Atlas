using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;

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
            [BlobTrigger("%AzureStorage:HaplotypeFrequencySetImportContainer%/{fullPath}", Connection = "AzureStorage:ConnectionString")]
            Stream blob,
            string fullPath,
            BlobProperties properties)
        {
            using (var file = new FrequencySetFile
            {
                Contents = blob,
                FullPath = fullPath,
                UploadedDateTime = properties.LastModified
            })
            {
                await frequencySetService.ImportFrequencySet(file);
            }
        }
    }
}