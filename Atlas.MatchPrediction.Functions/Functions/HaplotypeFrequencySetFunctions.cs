using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IFrequencySetService frequencySetService;
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        public HaplotypeFrequencySetFunctions(IFrequencySetService frequencySetService, IMatchPredictionAlgorithm matchPredictionAlgorithm)
        {
            this.frequencySetService = frequencySetService;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
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

        [FunctionName((nameof(GetHaplotypeFrequencySet)))]
        [StorageAccount("AzureStorage:ConnectionString")]
        public async Task<HaplotypeFrequencySet> GetHaplotypeFrequencySet([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request)
        {
            var donorInfo = new IndividualPopulationData
            {
                EthnicityId = request.Query["donorEthnicity"],
                RegistryId = request.Query["donorRegistry"]
            };
            
            var patientInfo = new IndividualPopulationData
            {
                EthnicityId = request.Query["patientEthnicity"],
                RegistryId = request.Query["patientRegistry"]
            };
            return await matchPredictionAlgorithm.GetHaplotypeFrequencySet(
                new HaplotypeFrequencySetInput
                {
                    DonorInfo = donorInfo,
                    PatientInfo = patientInfo
                }     );
        }
    }
}