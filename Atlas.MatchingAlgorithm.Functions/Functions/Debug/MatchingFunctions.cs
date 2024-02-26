using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.MatchingAlgorithm.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class MatchingFunctions
    {
        private const string RoutePrefix = $"{RouteConstants.DebugRoutePrefix}/matching/";

        private readonly IServiceBusPeeker<MatchingResultsNotification> notificationsPeeker;
        private readonly IMatchingResultsDownloader matchingResultsDownloader;

        public MatchingFunctions(
            IServiceBusPeeker<MatchingResultsNotification> notificationsPeeker,
            IMatchingResultsDownloader matchingResultsDownloader)
        {
            this.notificationsPeeker = notificationsPeeker;
            this.matchingResultsDownloader = matchingResultsDownloader;
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
        [ProducesResponseType(typeof(OriginalMatchingAlgorithmResultSet), (int)HttpStatusCode.OK)]
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
                var resultSet = await matchingResultsDownloader.DownloadResultSet(debugRequest.SearchResultFileName, debugRequest.BatchFolderName);
                return new JsonResult(resultSet);
            }
            catch (BlobNotFoundException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}