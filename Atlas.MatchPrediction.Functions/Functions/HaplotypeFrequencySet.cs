using Atlas.MatchPrediction.Services;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySet
    {
        private readonly IHaplotypeFrequencySetService haplotypeFrequencySetService;
        
        public HaplotypeFrequencySet(IHaplotypeFrequencySetService haplotypeFrequencySetService)
        {
            this.haplotypeFrequencySetService = haplotypeFrequencySetService;
        }

        [FunctionName(nameof(ImportHaplotypeFrequencySet))]
        public async Task ImportHaplotypeFrequencySet(
            [BlobTrigger("%AzureStorage:HaplotypeFrequencySetImportContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")] Stream blob,
            string fileName)
        {
            await haplotypeFrequencySetService.ImportHaplotypeFrequencySet(fileName, blob);
        }
    }
}
