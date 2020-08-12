using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.Functions.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using FluentValidation;
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

        public SearchClientFunctions(ILogger logger)
        {
            this.logger = logger;
        }
        
        [FunctionName(nameof(Search))]
        public async Task<HttpResponseMessage> Search(
            [HttpTrigger(AuthorizationLevel.Function,  "post")]
            [RequestBodyType(typeof(SearchRequest), nameof(SearchRequest))]
            HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await request.Content.ReadAsStringAsync());

            var matchingRequest = searchRequest.ToMatchingRequest();
            
            var validationResult = new SearchRequestValidator().Validate(matchingRequest);
            if (!validationResult.IsValid)
            {
                return new HttpResponseMessage( HttpStatusCode.BadRequest ) {Content =  new StringContent(JsonConvert.SerializeObject(validationResult.Errors), System.Text.Encoding.UTF8, "application/json" ) };
            }
            
            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.SearchOrchestrator), searchRequest);
            
            logger.SendTrace($"Started search orchestration with ID = '{instanceId}'.");

            // returns response including GET URL to fetch status, and eventual output, of orchestration function
            return starter.CreateCheckStatusResponse(request, instanceId);
        }
    }
}