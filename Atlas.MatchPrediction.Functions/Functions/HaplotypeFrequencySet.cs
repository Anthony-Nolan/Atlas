using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.WebJobs;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySet
    {
        private readonly IHaplotypeFrequencySetMetaDataService metaDataService;
        private readonly IHaplotypeFrequencySetImportService importService;
        
        public HaplotypeFrequencySet(
            IHaplotypeFrequencySetMetaDataService metaDataService,
            IHaplotypeFrequencySetImportService importService)
        {
            this.metaDataService = metaDataService;
            this.importService = importService;
        }

        [FunctionName(nameof(ImportHaplotypeFrequencySet))]
        public async Task ImportHaplotypeFrequencySet(
            [BlobTrigger("%AzureStorage:HaplotypeFrequencySetImportContainer%/{fileName}", Connection = "AzureStorage:ConnectionString")] Stream blob,
            string fileName)
        {
            var metaData = metaDataService.GetMetadataFromFileName(fileName);

            await importService.Import(blob, metaData);
        }
    }
}
