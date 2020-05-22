using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySet
    {
        private readonly IFrequencySetService frequencySetService;

        public HaplotypeFrequencySet(IFrequencySetService frequencySetService)
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