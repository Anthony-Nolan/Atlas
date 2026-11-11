using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class OriginalResultsListenerFunctions
    {
        private readonly IOriginalSearchResultSetTracker originalSearchResultSetTracker;
        private readonly IAtlasLogger logger;

        public OriginalResultsListenerFunctions(IOriginalSearchResultSetTracker originalSearchResultSetTracker, IAtlasLogger logger)
        {
            this.originalSearchResultSetTracker = originalSearchResultSetTracker;
            this.logger = logger;
        }
        
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(StoreOriginalSearchResults))]
        public async Task StoreOriginalSearchResults(
            [ServiceBusTrigger(
                "%MessagingServiceBus:OriginalSearchRequestsTopic%",
                "%MessagingServiceBus:OriginalSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            MatchingResultsNotification resultsNotification,
            int deliveryCount,
            DateTime enqueuedTimeUtc)
        {
            if (resultsNotification.WasSuccessful)
            {
                // ATL-526 experiment: one "Results pipeline timings" event per invocation.
                using var timings = ResultsPipelineTimings.Begin(
                    logger, nameof(StoreOriginalSearchResults), resultsNotification.SearchRequestId, null, deliveryCount, enqueuedTimeUtc);
                ResultsPipelineTimings.SetMetric("NotifiedResults", resultsNotification.NumberOfResults ?? 0);
                ResultsPipelineTimings.Set("ResultsBatched", resultsNotification.ResultsBatched.ToString());

                try
                {
                    await originalSearchResultSetTracker.StoreOriginalSearchResults(resultsNotification);
                }
                catch (Exception e)
                {
                    timings.Fail(e);
                    throw;
                }
            }
        }
    }
}