using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Services;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class ImportHaplotypeFrequencySet
    {
        private readonly IHaplotypeFrequencySetService haplotypeFrequencySetService;
        
        public ImportHaplotypeFrequencySet(IHaplotypeFrequencySetService haplotypeFrequencySetService)
        {
            this.haplotypeFrequencySetService = haplotypeFrequencySetService;
        }

        [FunctionName("ImportSetWithRegistryEthnicityFilename")]
        public async Task Run([BlobTrigger("haplotype-frequency-set-import/{fileName}")] Stream blob, string fileName)
        {
            await haplotypeFrequencySetService.ImportHaplotypeFrequencySet(fileName, blob);
        }
    }
}
