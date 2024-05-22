using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Atlas.Functions.Services.Debug;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.Functions.Functions.Debug
{
    public class RepeatSearchFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/repeatSearch/";

        private readonly IRepeatSearchResultNotificationsPeeker notificationsPeeker;
        private readonly IDebugResultsDownloader resultsDownloader;

        public RepeatSearchFunctions(
            IRepeatSearchResultNotificationsPeeker notificationsPeeker,
            IDebugResultsDownloader resultsDownloader)
        {
            this.notificationsPeeker = notificationsPeeker;
            this.resultsDownloader = resultsDownloader;
        }

        [Function(nameof(PeekRepeatSearchResultsNotifications))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<SearchResultsNotification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekRepeatSearchResultsNotifications(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RoutePrefix}notifications")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await notificationsPeeker.Peek(peekRequest);
            return new JsonResult(response);
        }

        [Function(nameof(FetchRepeatSearchResultSet))]
        [ProducesResponseType(typeof(RepeatSearchResultSet), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> FetchRepeatSearchResultSet(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RoutePrefix}resultSet/")]
            [RequestBodyType(typeof(DebugSearchResultsRequest), nameof(DebugSearchResultsRequest))]
            HttpRequest request)
        {
            try
            {
                var debugRequest = await request.DeserialiseRequestBody<DebugSearchResultsRequest>();
                var resultSet = await resultsDownloader.DownloadResultSet<RepeatSearchResultSet, SearchResult>(debugRequest);
                return new JsonResult(resultSet);
            }
            catch (BlobNotFoundException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}