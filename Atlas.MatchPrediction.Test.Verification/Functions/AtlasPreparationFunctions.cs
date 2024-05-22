using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Services;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class AtlasPreparationFunctions
    {
        private readonly IVerificationAtlasPreparer atlasPreparer;
        private readonly ITestHarnessRepository testHarnessRepository;

        public AtlasPreparationFunctions(IVerificationAtlasPreparer atlasPreparer, ITestHarnessRepository testHarnessRepository)
        {
            this.atlasPreparer = atlasPreparer;
            this.testHarnessRepository = testHarnessRepository;
        }

        [Function(nameof(PrepareAtlasDonorStores))]
        public async Task PrepareAtlasDonorStores(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(TestHarnessDetails), nameof(TestHarnessDetails))]
            HttpRequest request)
        {
            try
            {
                var testHarnessId = JsonConvert.DeserializeObject<TestHarnessDetails>(
                    await new StreamReader(request.Body).ReadToEndAsync()).TestHarnessId;

                if (await TestHarnessNotCompleted(testHarnessId))
                {
                    throw new ArgumentException($"Cannot export donors for test harness {testHarnessId} as it is marked as incomplete.");
                }

                await atlasPreparer.PrepareAtlasWithTestHarnessDonors(testHarnessId);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(HttpStatusCode.InternalServerError, "Failed to prepare Atlas donor stores.", ex);
            }
        }

        [Function(nameof(HandleDataRefreshCompletion))]
        public async Task HandleDataRefreshCompletion(
            [ServiceBusTrigger(
                "%DataRefresh:CompletionTopic%",
                "%DataRefresh:CompletionTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            CompletedDataRefresh dataRefresh)
        {
            await atlasPreparer.SaveDataRefreshDetails(dataRefresh);
        }

        private async Task<bool> TestHarnessNotCompleted(int testHarnessId)
        {
            return !(await testHarnessRepository.GetTestHarness(testHarnessId)).WasCompleted;
        }
    }
}