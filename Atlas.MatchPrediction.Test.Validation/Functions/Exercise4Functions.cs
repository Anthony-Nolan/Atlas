using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Validation.Services.Exercise4;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Functions
{
    /// <summary>
    /// Functions required for running of exercise 4 of the WMDA BioInformatics and Innovation Working Group.
    /// </summary>
    public class Exercise4Functions
    {
        private const string FunctionNamePrefix = "Exercise4_";
        private readonly IValidationAtlasPreparer atlasPreparer;

        public Exercise4Functions(IValidationAtlasPreparer atlasPreparer)
        {
            this.atlasPreparer = atlasPreparer;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName($"{FunctionNamePrefix}{nameof(PrepareAtlasDonorStores)}")]
        public async Task PrepareAtlasDonorStores(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            try
            {
                await atlasPreparer.PrepareAtlasWithImportedDonors();
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to prepare Atlas donor stores.", ex);
            }
        }

        [FunctionName($"{FunctionNamePrefix}{nameof(HandleDataRefreshCompletion)}")]
        public async Task HandleDataRefreshCompletion(
        [ServiceBusTrigger(
                "%DataRefresh:CompletionTopic%",
                "%DataRefresh:CompletionTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            CompletedDataRefresh dataRefresh)
        {
            await atlasPreparer.UpdateLatestExportRecordWithDataRefreshDetails(dataRefresh);
        }
    }
}