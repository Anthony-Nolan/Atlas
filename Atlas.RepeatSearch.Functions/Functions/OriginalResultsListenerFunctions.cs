using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class OriginalResultsListenerFunctions
    {
        private readonly IOriginalSearchResultsListener originalSearchResultsListener;

        public OriginalResultsListenerFunctions(IOriginalSearchResultsListener originalSearchResultsListener)
        {
            this.originalSearchResultsListener = originalSearchResultsListener;
        }
        
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(StoreOriginalSearchResults))]
        public async Task StoreOriginalSearchResults(
            [ServiceBusTrigger(
                "%MessagingServiceBus:OriginalSearchRequestsTopic%",
                "%MessagingServiceBus:OriginalSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            Message message)
        {
            var serialisedData = Encoding.UTF8.GetString(message.Body);
            var resultsNotification = JsonConvert.DeserializeObject<MatchingResultsNotification>(serialisedData);

            await originalSearchResultsListener.StoreOriginalSearchResults(resultsNotification);
        }
    }
}