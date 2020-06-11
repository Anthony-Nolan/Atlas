using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IFrequencySetService frequencySetService;

        public HaplotypeFrequencySetFunctions(IFrequencySetService frequencySetService)
        {
            this.frequencySetService = frequencySetService;
        }

        /// IMPORTANT: Do not rename this function without careful consideration. This function is called by event grid, which has the function name set by terraform.
        // If changing this, you must also change the value hardcoded in the event_grid.tf terraform file. 
        [FunctionName(nameof(ImportHaplotypeFrequencySet))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task ImportHaplotypeFrequencySet(
            [EventGridTrigger] EventGridEvent blobCreatedEvent,
            [Blob("{data.url}", FileAccess.Read)] Stream blobStream
        )
        {
            using (var file = new FrequencySetFile
            {
                Contents = blobStream,
                FullPath = blobCreatedEvent.Subject,
                UploadedDateTime = blobCreatedEvent.EventTime
            })
            {
                await frequencySetService.ImportFrequencySet(file);
            }
        }
    }
}