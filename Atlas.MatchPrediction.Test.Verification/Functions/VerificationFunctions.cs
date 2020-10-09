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

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class VerificationFunctions
    {
        private readonly IVerificationRunner verificationRunner;
        private readonly ISearchResultSetProcessor searchResultSetProcessor;
        private readonly IVerificationResultsWriter resultsWriter;

        public VerificationFunctions(
            IVerificationRunner verificationRunner,
            ISearchResultSetProcessor searchResultSetProcessor,
            IVerificationResultsWriter resultsWriter)
        {
            this.verificationRunner = verificationRunner;
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

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(FetchSearchResults))]
        public async Task FetchSearchResults(
            [ServiceBusTrigger(
                "%Search:ResultsTopic%",
                "%Search:ResultsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            Message message)
        {
            var serialisedData = Encoding.UTF8.GetString(message.Body);
            var notification = JsonConvert.DeserializeObject<SearchResultsNotification>(serialisedData);

            try
            {
                await searchResultSetProcessor.ProcessAndStoreSearchResultSet(notification);
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