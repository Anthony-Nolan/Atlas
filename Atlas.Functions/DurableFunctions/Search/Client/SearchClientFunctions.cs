using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.DurableFunctions.Search.Client
{
    /// <summary>
    /// See https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-types-features-overview for the types of durable functions available
    /// We are using:
    ///     - Client = entry points.
    ///     - Orchestrator = orchestration. DETERMINISTIC (https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints)
    ///     - Activity = business logic. IDEMPOTENT (may be called multiple times, only at least once execution is guaranteed by Azure)
    /// </summary>
    public class SearchClientFunctions
    {
        private readonly ILogger logger;
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        public SearchClientFunctions(IMatchPredictionAlgorithm matchPredictionAlgorithm, ILogger logger)
        {
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.logger = logger;
        }

        [FunctionName(nameof(Search))]
        public async Task Search(
            [ServiceBusTrigger(
                "%AtlasFunction:MessagingServiceBus:MatchingResultsTopic%",
                "%AtlasFunction:MessagingServiceBus:MatchingResultsSubscription%",
                Connection = "AtlasFunction:MessagingServiceBus:ConnectionString"
            )]
            MatchingResultsNotification resultsNotification,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            var searchRequest = resultsNotification.SearchRequest;

            // var matchingRequest = searchRequest;
            
            // TODO: ATLAS-665: Move validation to top level layer.
            // var validationResult = await new SearchRequestValidator().ValidateAsync(matchingRequest);
            // if (!validationResult.IsValid)
            // {
                // return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    // {Content = new StringContent(JsonConvert.SerializeObject(validationResult.Errors), Encoding.UTF8, "application/json")};
            // }

            // var probabilityRequestToValidate = searchRequest.ToPartialMatchProbabilitySearchRequest();
            // var probabilityValidationResult = matchPredictionAlgorithm.ValidateMatchPredictionAlgorithmInput(probabilityRequestToValidate);
            // if (!probabilityValidationResult.IsValid)
            // {
                // return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    // {Content = new StringContent(JsonConvert.SerializeObject(probabilityValidationResult.Errors), Encoding.UTF8, "application/json")};
            // }
            
            
            // Function input comes from the request content.
            // TODO: ATLAS-665: Use custom search request id
            var instanceId = await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.SearchOrchestrator), searchRequest);

            logger.SendTrace($"Started search orchestration with ID = '{instanceId}'.");

            // TODO: ATLAS-665: Return this from initiation endpoint?
            // returns response including GET URL to fetch status, and eventual output, of orchestration function
        }
    }
}