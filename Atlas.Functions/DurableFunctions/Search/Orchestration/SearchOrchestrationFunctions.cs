using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Orchestration
{
    public class SearchOrchestrationFunctions
    {
        [FunctionName(nameof(SearchOrchestrator))]
        public async Task<List<string>> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var input = context.GetInput<SearchRequest>();
            
            var outputs = new List<string>
            {
                await context.CallActivityAsync<string>(nameof(SearchActivityFunctions.SayHello), "Tokyo"),
                await context.CallActivityAsync<string>(nameof(SearchActivityFunctions.SayHello), "Seattle"),
                await context.CallActivityAsync<string>(nameof(SearchActivityFunctions.SayHello), "London")
            };

            // Replace "hello" with the name of your Durable Activity Function.

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
            
        }
    }
}