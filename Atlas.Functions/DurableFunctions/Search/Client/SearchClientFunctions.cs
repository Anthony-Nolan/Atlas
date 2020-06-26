using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.DurableFunctions.Search.Client
{
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
            HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await request.Content.ReadAsStringAsync());

            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.SearchOrchestrator), searchRequest);
            
            logger.SendTrace($"Started search orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(request, instanceId);
        }
    }
}