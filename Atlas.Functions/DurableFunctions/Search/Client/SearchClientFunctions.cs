using System;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.MatchPrediction.ExternalInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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
            var searchId = resultsNotification.SearchRequestId;
            await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.SearchOrchestrator), resultsNotification);

            try
            {
                logger.SendTrace($"Started match prediction orchestration with ID = '{searchId}'.");
                var statusCheck = await GetStatusCheckEndpoints(starter, searchId);
                logger.SendTrace(statusCheck.StatusQueryGetUri);
            }
            catch (Exception)
            {
                // This function cannot be allowed to fail post-orchestration scheduling, as it would then retry, and we cannot schedule more than one orchestrator with the same id.
                // We are only doing logging past this point, so if it fails we just swallow exceptions.
            }
        }

        private static async Task<StatusCheckEndpoints> GetStatusCheckEndpoints(IDurableOrchestrationClient orchestrationClient, string searchId)
        {
            // Log status check endpoints for convenience of debugging long search requests
            var checkStatusResponse = orchestrationClient.CreateCheckStatusResponse(new HttpRequestMessage(), searchId);
            return JsonConvert.DeserializeObject<StatusCheckEndpoints>(await checkStatusResponse.Content.ReadAsStringAsync());
        }

        private class StatusCheckEndpoints
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string StatusQueryGetUri { get; set; }
        }
    }
}