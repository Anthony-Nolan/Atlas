using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.RepeatSearch.Functions.Functions.Debug
{
    public class RepeatSearchFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/repeatSearch/";

        private readonly IServiceBusPeeker<MatchingResultsNotification> notificationsPeeker;
        private readonly IDebugResultsDownloader resultsDownloader;

        public RepeatSearchFunctions(
            IServiceBusPeeker<MatchingResultsNotification> notificationsPeeker,
            IDebugResultsDownloader resultsDownloader)
        {
            this.notificationsPeeker = notificationsPeeker;
            this.resultsDownloader = resultsDownloader;
        }

        [FunctionName(nameof(PeekMatchingResultNotifications))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<MatchingResultsNotification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekMatchingResultNotifications(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RoutePrefix}notifications/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await notificationsPeeker.Peek(peekRequest);
            return new JsonResult(response);
        }

        [FunctionName(nameof(FetchMatchingResultSet))]
        [ProducesResponseType(typeof(RepeatMatchingAlgorithmResultSet), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> FetchMatchingResultSet(
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
                var resultSet = await resultsDownloader.DownloadResultSet<RepeatMatchingAlgorithmResultSet, MatchingAlgorithmResult>(debugRequest);
                return new JsonResult(resultSet);
            }
            catch (BlobNotFoundException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}