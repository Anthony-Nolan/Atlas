using System;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.Functions.Models;
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

        public SearchClientFunctions(ILogger logger)
        { 
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
            [DurableClient] IDurableOrchestrationClient starter,
            DateTime enqueuedTimeUtc)
        {
            var searchId = resultsNotification.SearchRequestId;
            if (!resultsNotification.SearchRequest?.RunMatchPrediction ?? false)
            {
                logger.SendTrace($"Match prediction for search request '{searchId}' was not requested, so will not be run.");
                // TODO: ATLAS-493: Make the matching only results usable. 
                return;
            }

            var instanceId = await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.SearchOrchestrator), new SearchOrchestratorParameters{ MatchingResultsNotification = resultsNotification, InitiationTime = enqueuedTimeUtc });

            try
            {
                logger.SendTrace($"Started match prediction orchestration with ID = '{searchId}'. Orchestration Instance: {instanceId}");
                var statusCheck = await GetStatusCheckEndpoints(starter, instanceId);
                logger.SendTrace(statusCheck.StatusQueryGetUri);
            }
            catch (Exception)
            {
                // This function cannot be allowed to fail post-orchestration scheduling, as it would then retry, and we cannot schedule more than one orchestrator with the same id.
                // We are only doing logging past this point, so if it fails we just swallow exceptions.
            }
        }

        [FunctionName(nameof(RepeatSearch))]
        public async Task RepeatSearch(
            [ServiceBusTrigger(
                "%AtlasFunction:MessagingServiceBus:RepeatSearchMatchingResultsTopic%",
                "%AtlasFunction:MessagingServiceBus:RepeatSearchMatchingResultsSubscription%",
                Connection = "AtlasFunction:MessagingServiceBus:ConnectionString"
            )]
            MatchingResultsNotification resultsNotification,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            var repeatSearchId = resultsNotification.RepeatSearchRequestId;
            if (!resultsNotification.SearchRequest?.RunMatchPrediction ?? false)
            {
                logger.SendTrace($"Match prediction for repeat search request '{repeatSearchId}' was not requested, so will not be run.");
                // TODO: ATLAS-493: Make the matching only results usable. 
                return;
            }

            var instanceId = await starter.StartNewAsync(nameof(SearchOrchestrationFunctions.RepeatSearchOrchestrator), resultsNotification);

            try
            {
                logger.SendTrace($"Started match prediction orchestration with Repeat Search ID = '{repeatSearchId}'. Orchestration Instance: {instanceId}");
                var statusCheck = await GetStatusCheckEndpoints(starter, instanceId);
                logger.SendTrace(statusCheck.StatusQueryGetUri);
            }
            catch (Exception)
            {
                // This function cannot be allowed to fail post-orchestration scheduling, as it would then retry, and we cannot schedule more than one orchestrator with the same id.
                // We are only doing logging past this point, so if it fails we just swallow exceptions.
            }
        }

        private static async Task<StatusCheckEndpoints> GetStatusCheckEndpoints(IDurableOrchestrationClient orchestrationClient, string instanceId)
        {
            // Log status check endpoints for convenience of debugging long search requests
            var checkStatusResponse = orchestrationClient.CreateCheckStatusResponse(new HttpRequestMessage(), instanceId);
            return JsonConvert.DeserializeObject<StatusCheckEndpoints>(await checkStatusResponse.Content.ReadAsStringAsync());
        }

        private class StatusCheckEndpoints
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string StatusQueryGetUri { get; set; }
        }
    }
}