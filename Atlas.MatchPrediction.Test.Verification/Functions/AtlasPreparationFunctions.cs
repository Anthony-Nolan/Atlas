using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class AtlasPreparationFunctions
    {
        private readonly IAtlasPreparer atlasPreparer;

        public AtlasPreparationFunctions(IAtlasPreparer atlasPreparer)
        {
            this.atlasPreparer = atlasPreparer;
        }

        [FunctionName(nameof(PrepareAtlasDonorStores))]
        public async Task PrepareAtlasDonorStores(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(TestHarnessDetails), nameof(TestHarnessDetails))]
            HttpRequest request)
        {
            try
            {
                var testHarnessDetails = JsonConvert.DeserializeObject<TestHarnessDetails>(
                    await new StreamReader(request.Body).ReadToEndAsync());

                await atlasPreparer.PrepareAtlasDonorStores(testHarnessDetails.TestHarnessId);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to prepare Atlas donor stores.", ex);
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(HandleDataRefreshCompletion))]
        public async Task HandleDataRefreshCompletion(
            [ServiceBusTrigger(
                "%DataRefresh:CompletionTopic%",
                "%DataRefresh:CompletionTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            CompletedDataRefresh dataRefresh)
        {
            await atlasPreparer.UpdateLatestExportRecord(dataRefresh);
        }
    }
}