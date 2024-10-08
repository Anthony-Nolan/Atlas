﻿using System;
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

        private const string DonorEthnicityQueryParam = "donorEthnicity";
        private const string DonorRegistryQueryParam = "donorRegistry";
        private const string PatientEthnicityQueryParam = "patientEthnicity";

        public HaplotypeFrequencySetFunctions(IHaplotypeFrequencyService haplotypeFrequencyService,
            IMatchPredictionAlgorithm matchPredictionAlgorithm)
        {
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
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