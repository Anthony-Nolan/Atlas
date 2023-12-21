using Atlas.Client.Models.Search.Results;
using Atlas.Common.Utils;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class VerificationFunctions
    {
        private readonly IVerificationRunner verificationRunner;
        private readonly IResultSetProcessor<MatchingResultsNotification> matchingGenotypesProcessor;
        private readonly IResultSetProcessor<SearchResultsNotification> searchResultSetProcessor;
        private readonly IVerificationResultsWriter resultsWriter;

        public VerificationFunctions(
            IVerificationRunner verificationRunner,
            IResultSetProcessor<MatchingResultsNotification> matchingGenotypesProcessor,
            IResultSetProcessor<SearchResultsNotification> searchResultSetProcessor,
            IVerificationResultsWriter resultsWriter)
        {
            this.verificationRunner = verificationRunner;
            this.matchingGenotypesProcessor = matchingGenotypesProcessor;
            this.searchResultSetProcessor = searchResultSetProcessor;
            this.resultsWriter = resultsWriter;
        }

        [FunctionName(nameof(SendVerificationSearchRequests))]
        public async Task<IActionResult> SendVerificationSearchRequests(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(TestHarnessDetails), nameof(TestHarnessDetails))]
            HttpRequest request)
        {
            var testHarnessDetails = JsonConvert.DeserializeObject<TestHarnessDetails>(
                    await new StreamReader(request.Body).ReadToEndAsync());

            var verificationRunId = await verificationRunner.SendVerificationSearchRequests(testHarnessDetails.TestHarnessId);

            return new JsonResult(verificationRunId);
        }

        [FunctionName(nameof(FetchMatchingResults))]
        public async Task FetchMatchingResults(
            [ServiceBusTrigger(
                "%Matching:ResultsTopic%",
                "%Matching:ResultsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            MatchingResultsNotification notification)
        {
            try
            {
                await matchingGenotypesProcessor.ProcessAndStoreResultSet(notification);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error while downloading results for {notification.SearchRequestId}: {ex.GetBaseException()}");
                throw;
            }
        }

        [FunctionName(nameof(FetchSearchResults))]
        public async Task FetchSearchResults(
            [ServiceBusTrigger(
                "%Search:ResultsTopic%",
                "%Search:ResultsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            SearchResultsNotification notification)
        {
            try
            {
                await searchResultSetProcessor.ProcessAndStoreResultSet(notification);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error while downloading results for {notification.SearchRequestId}: {ex.GetBaseException()}");
                throw;
            }
        }

        [FunctionName(nameof(WriteVerificationResultsToFile))]
        public async Task WriteVerificationResultsToFile(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(VerificationResultsRequest), nameof(VerificationResultsRequest))]
            HttpRequest request)
        {
            var resultsRequest = JsonConvert.DeserializeObject<VerificationResultsRequest>(
                await new StreamReader(request.Body).ReadToEndAsync());

            await resultsWriter.WriteVerificationResultsToFile(resultsRequest);
        }
    }
}