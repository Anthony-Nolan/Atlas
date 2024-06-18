using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.Functions.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.Functions.Functions.Debug
{
    public class SearchFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/search/";

        private readonly ISearchResultNotificationsPeeker notificationsPeeker;
        private readonly IDebugResultsDownloader resultsDownloader;

        public SearchFunctions(
            ISearchResultNotificationsPeeker notificationsPeeker,
            IDebugResultsDownloader resultsDownloader)
        {
            this.notificationsPeeker = notificationsPeeker;
            this.resultsDownloader = resultsDownloader;
        }

        [Function(nameof(PeekSearchResultsNotifications))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<SearchResultsNotification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekSearchResultsNotifications(
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

        [Function(nameof(FetchSearchResultSet))]
        [ProducesResponseType(typeof(OriginalSearchResultSet), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> FetchSearchResultSet(
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
                var resultSet = await resultsDownloader.DownloadResultSet<OriginalSearchResultSet, SearchResult>(debugRequest);
                return new JsonResult(resultSet);
            }
            catch (BlobNotFoundException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}