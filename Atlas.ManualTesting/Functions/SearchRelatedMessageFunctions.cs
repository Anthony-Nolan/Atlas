using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.ManualTesting.Functions
{
    public class SearchRelatedMessageFunctions
    {
        private readonly IMatchingRequestsPeeker matchingRequestsPeeker;
        private readonly ISearchResultNotificationsPeeker notificationsPeeker;

        public SearchRelatedMessageFunctions(
            IMatchingRequestsPeeker matchingRequestsPeeker,
            ISearchResultNotificationsPeeker notificationsPeeker)
        {
            this.matchingRequestsPeeker = matchingRequestsPeeker;
            this.notificationsPeeker = notificationsPeeker;
        }

        [Function(nameof(GetDeadLetteredMatchingRequestIds))]
        public async Task<IActionResult> GetDeadLetteredMatchingRequestIds(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();

            var ids = await matchingRequestsPeeker.GetIdsOfDeadLetteredMatchingRequests(peekRequest);

            return new JsonResult(ids);
        }

        [Function(nameof(GetFailedSearchIds))]
        public async Task<IActionResult> GetFailedSearchIds(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();

            var ids = await notificationsPeeker.GetIdsOfFailedSearches(peekRequest);

            return new JsonResult(ids);
        }

        [Function(nameof(GetSearchResultsNotificationsWithSummary))]
        public async Task<IActionResult> GetSearchResultsNotificationsWithSummary(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();

            var resultsNotifications = await notificationsPeeker.GetSearchResultsNotifications(peekRequest);

            return new JsonResult(resultsNotifications);
        }

        [Function(nameof(FilterSearchResultsNotificationsBySearchRequestId))]
        public async Task<IActionResult> FilterSearchResultsNotificationsBySearchRequestId(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekBySearchRequestIdRequest), nameof(PeekBySearchRequestIdRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekBySearchRequestIdRequest>();

            var resultsNotifications = await notificationsPeeker.GetNotificationsBySearchRequestId(peekRequest);

            return new JsonResult(resultsNotifications);
        }
    }
}
