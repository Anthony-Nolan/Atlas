using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class OriginalResultsListenerFunctions
    {
        private readonly IOriginalSearchResultSetTracker originalSearchResultSetTracker;

        public OriginalResultsListenerFunctions(IOriginalSearchResultSetTracker originalSearchResultSetTracker)
        {
            this.originalSearchResultSetTracker = originalSearchResultSetTracker;
        }
        
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(StoreOriginalSearchResults))]
        public async Task StoreOriginalSearchResults(
            [ServiceBusTrigger(
                "%MessagingServiceBus:OriginalSearchRequestsTopic%",
                "%MessagingServiceBus:OriginalSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            MatchingResultsNotification resultsNotification)
        {
            if (resultsNotification.WasSuccessful)
            {
                await originalSearchResultSetTracker.StoreOriginalSearchResults(resultsNotification);
            }
        }
    }
}