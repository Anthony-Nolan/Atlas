using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using Atlas.SearchTracking.Common.Config;

namespace Atlas.SearchTracking.Functions.Functions
{
    public class SearchTrackingFunctions
    {
        private readonly ISearchTrackingEventProcessor searchTrackingEventProcessor;

        public SearchTrackingFunctions(ISearchTrackingEventProcessor searchTrackingEventProcessor)
        {
            this.searchTrackingEventProcessor = searchTrackingEventProcessor;
        }

        [Function(nameof(HandleSearchTrackingEvent))]
        public async Task HandleSearchTrackingEvent(
            [ServiceBusTrigger("%MessagingServiceBus:SearchTrackingTopic%",
                "%MessagingServiceBus:SearchTrackingSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
                ServiceBusReceivedMessage message)
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var eventType = Enum.Parse<SearchTrackingEventType>(message.ApplicationProperties.GetValueOrDefault(SearchTrackingConstants.EventType).ToString());

            await searchTrackingEventProcessor.HandleEvent(body, eventType);
        }
    }
}