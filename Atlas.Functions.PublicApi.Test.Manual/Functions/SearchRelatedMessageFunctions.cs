using System.IO;
using System.Threading.Tasks;
using Atlas.Functions.PublicApi.Test.Manual.Models;
using Atlas.Functions.PublicApi.Test.Manual.Services;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Test.Manual.Functions
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

        [FunctionName(nameof(GetDeadLetteredMatchingRequestIds))]
        public async Task<IActionResult> GetDeadLetteredMatchingRequestIds(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekRequest), nameof(PeekRequest))]
            HttpRequest request)
        {
            var peekRequest = await DeserialiseRequestBody<PeekRequest>(request);

            var ids = await matchingRequestsPeeker.GetIdsOfDeadLetteredMatchingRequests(peekRequest);

            return new JsonResult(ids);
        }

        [FunctionName(nameof(GetFailedSearchIds))]
        public async Task<IActionResult> GetFailedSearchIds(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekRequest), nameof(PeekRequest))]
            HttpRequest request)
        {
            var peekRequest = await DeserialiseRequestBody<PeekRequest>(request);

            var ids = await notificationsPeeker.GetIdsOfFailedSearches(peekRequest);

            return new JsonResult(ids);
        }

        [FunctionName(nameof(GetSearchResultsNotificationsWithSummary))]
        public async Task<IActionResult> GetSearchResultsNotificationsWithSummary(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekRequest), nameof(PeekRequest))]
            HttpRequest request)
        {
            var peekRequest = await DeserialiseRequestBody<PeekRequest>(request);

            var resultsNotifications = await notificationsPeeker.GetSearchResultsNotifications(peekRequest);

            return new JsonResult(resultsNotifications);
        }

        [FunctionName(nameof(FilterSearchResultsNotificationsBySearchRequestId))]
        public async Task<IActionResult> FilterSearchResultsNotificationsBySearchRequestId(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PeekBySearchRequestIdRequest), nameof(PeekBySearchRequestIdRequest))]
            HttpRequest request)
        {
            var peekRequest = await DeserialiseRequestBody<PeekBySearchRequestIdRequest>(request);

            var resultsNotifications = await notificationsPeeker.GetNotificationsBySearchRequestId(peekRequest);

            return new JsonResult(resultsNotifications);
        }

        private static async Task<T> DeserialiseRequestBody<T>(HttpRequest request)
        {
            return JsonConvert.DeserializeObject<T>(
                await new StreamReader(request.Body).ReadToEndAsync());
        }
    }
}
