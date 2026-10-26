using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.AzureEventGrid;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HaplotypeFrequencySetFunctions
    {
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private readonly IFrequencySetContentHashBackfill contentHashBackfill;
        private readonly IFrequencySetContentHashBackfillStarter contentHashBackfillStarter;

        private const string DonorEthnicityQueryParam = "donorEthnicity";
        private const string DonorRegistryQueryParam = "donorRegistry";
        private const string PatientEthnicityQueryParam = "patientEthnicity";

        public HaplotypeFrequencySetFunctions(IHaplotypeFrequencyService haplotypeFrequencyService,
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IFrequencySetContentHashBackfill contentHashBackfill,
            IFrequencySetContentHashBackfillStarter contentHashBackfillStarter)
        {
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.contentHashBackfill = contentHashBackfill;
            this.contentHashBackfillStarter = contentHashBackfillStarter;
        }

        /// IMPORTANT: Do not rename this function without careful consideration. This function is called by event grid, which has the function name set by terraform.
        // If changing this, you must also change the value hardcoded in the event_grid.tf terraform file. 
        [Function(nameof(ImportHaplotypeFrequencySet))]
        public async Task ImportHaplotypeFrequencySet(
            [ServiceBusTrigger(
                "%MessagingServiceBus:ImportFileTopic%",
                "%MessagingServiceBus:ImportFileSubscription%",
                Connection = "MessagingServiceBus:ConnectionString"
            )] EventGridSchema blobCreatedEvent,
            [BlobInput("{data.url}", Connection = "AzureStorage:ConnectionString")] Stream blobStream
        )
        {
            using (var file = new FrequencySetFile
            {
                Contents = blobStream,
                FileName = blobCreatedEvent.Subject.Split("/").Last(),
                UploadedDateTime = blobCreatedEvent.EventTime
            })
            {
                await haplotypeFrequencyService.ImportFrequencySet(file);
            }
        }

        /// <summary>
        /// Reads the active haplotype frequency sets from the database, and writes them to the distributed cache, as an import
        /// does. Call it after a change of the active sets with SQL (see README_Support.md): without it, other processes can
        /// use the replaced sets until the sets in the distributed cache expire. It writes twice, 30 seconds apart, so it
        /// returns after 30 seconds or more.
        /// </summary>
        [Function(nameof(RefreshActiveHaplotypeFrequencySetCache))]
        public async Task<IActionResult> RefreshActiveHaplotypeFrequencySetCache(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            return await haplotypeFrequencyService.RefreshActiveHaplotypeFrequencySetCache()
                ? new OkResult()
                : new ObjectResult("The distributed cache did not store the active haplotype frequency sets. Call again later.")
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
        }

        /// <summary>
        /// Writes the content hash of each haplotype frequency set that has none: the sets of the imports from before content
        /// hashes (see README_Support.md). Sets with the same hash share one entry in the caches. The work reads the
        /// frequencies of each such set, so it runs in the background of this app: the call starts it, and returns 202 with
        /// the counts. Call it again to see the progress: 200 means that every set has a hash. When the app restarts during
        /// the work, the work stops, and the next call continues it.
        /// </summary>
        [Function(nameof(BackfillHaplotypeFrequencySetContentHashes))]
        public async Task<IActionResult> BackfillHaplotypeFrequencySetContentHashes(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var counts = await contentHashBackfill.GetCounts();
            var done = counts.SetsWithNoContentHash == 0;

            if (!done)
            {
                contentHashBackfillStarter.Start();
            }

            var status = new FrequencySetContentHashBackfillStatus(
                counts.Sets,
                counts.SetsWithNoContentHash,
                counts.ContentHashesOfActiveSets,
                counts.ContentHashes,
                contentHashBackfillStarter.IsRunning);

            return done
                ? new OkObjectResult(status)
                : new ObjectResult(status) { StatusCode = StatusCodes.Status202Accepted };
        }

        [QueryStringParameter(DonorEthnicityQueryParam, "Ethnicity ID of the donor", DataType = typeof(string))]
        [QueryStringParameter(DonorRegistryQueryParam, "Registry ID of the donor", DataType = typeof(string))]
        [QueryStringParameter(PatientEthnicityQueryParam, "Ethnicity ID of the patient", DataType = typeof(string))]
        [Function((nameof(GetHaplotypeFrequencySet)))]
        public async Task<IActionResult> GetHaplotypeFrequencySet(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            try
            {
                var donorInfo = new FrequencySetMetadata
                {
                    EthnicityCode = request.Query[DonorEthnicityQueryParam],
                    RegistryCode = request.Query[DonorRegistryQueryParam]
                };

                var patientInfo = new FrequencySetMetadata
                {
                    EthnicityCode = request.Query[PatientEthnicityQueryParam],
                };
                var result = await matchPredictionAlgorithm.GetHaplotypeFrequencySet(
                    new HaplotypeFrequencySetInput
                    {
                        DonorInfo = donorInfo,
                        PatientInfo = patientInfo
                    });
                return new JsonResult(result);
            }
            catch(Exception exception)
            {
                return new ObjectResult(exception.Message)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}