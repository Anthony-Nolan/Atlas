using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using System.Text;

namespace Atlas.SearchTracking.Functions
{
    public class SearchTrackingFunctions
    {
        private readonly ISearchTrackingProcess searchTrackingProcess;

        public SearchTrackingFunctions(ISearchTrackingProcess searchTrackingProcess)
        {
            this.searchTrackingProcess = searchTrackingProcess;
        }

        [Function(nameof(HandleSearchTrackingEvent))]
        public async Task HandleSearchTrackingEvent(
            [ServiceBusTrigger("search-tracking-events", "search-tracking",
                Connection = "MessagingServiceBus:ConnectionString")]
                ServiceBusReceivedMessage message)
        {
            var body = Encoding.UTF8.GetString(message.Body);
            Enum.TryParse(message.ApplicationProperties.GetValueOrDefault("SearchTrackingEventType")!.ToString(),
                out SearchTrackingEventType eventType);

            await searchTrackingProcess.HandleEvent(body, eventType);
        }
    }
}
