using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using Atlas.Debug.Client.Models.SearchTracking;
using Atlas.SearchTracking.Common.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Atlas.Client.Models.Search.Results;

namespace Atlas.SearchTracking.Functions.Functions
{
    public class SearchTrackingFunctions
    {
        private readonly ISearchTrackingEventProcessor searchTrackingEventProcessor;
        private readonly ISearchTrackingDebugService searchTrackingDebugService;

        public SearchTrackingFunctions(ISearchTrackingEventProcessor searchTrackingEventProcessor, ISearchTrackingDebugService searchTrackingDebugService)
        {
            this.searchTrackingEventProcessor = searchTrackingEventProcessor;
            this.searchTrackingDebugService = searchTrackingDebugService;
        }

        [Function(nameof(HandleSearchTrackingEvent))]
        public async Task HandleSearchTrackingEvent(
            [ServiceBusTrigger("%MessagingServiceBus:SearchTrackingTopic%",
                "%MessagingServiceBus:SearchTrackingSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            ServiceBusReceivedMessage message)
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var eventType =
                Enum.Parse<SearchTrackingEventType>(message.ApplicationProperties.GetValueOrDefault(SearchTrackingConstants.EventType).ToString());

            await searchTrackingEventProcessor.HandleEvent(body, eventType);
        }

        [Function(nameof(GetSearchRequestByIdentifier))]
        public async Task<IActionResult> GetSearchRequestByIdentifier(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var searchIdentifier = Guid.Parse(request.Query["searchIdentifier"]);
            var searchRequest = await searchTrackingDebugService.GetSearchRequestByIdentifier(searchIdentifier);

            return new JsonResult(searchRequest);
        }
    }
}