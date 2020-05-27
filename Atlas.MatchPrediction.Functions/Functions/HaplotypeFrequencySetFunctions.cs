using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;

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
            string fileName)
        {
            await frequencySetService.ImportFrequencySet(blob, fileName);
        }
    }
}