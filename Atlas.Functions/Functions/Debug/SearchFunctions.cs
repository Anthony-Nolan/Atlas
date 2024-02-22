using Atlas.Client.Models.Search.Results;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ServiceBus;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.Functions.Functions.Debug
{
    public class SearchFunctions
    {
        private readonly IServiceBusPeeker<SearchResultsNotification> notificationsPeeker;

        public SearchFunctions(IServiceBusPeeker<SearchResultsNotification> notificationsPeeker)
        {
            this.notificationsPeeker = notificationsPeeker;
        }

        [FunctionName(nameof(PeekSearchResultsNotifications))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<SearchResultsNotification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekSearchResultsNotifications(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/search/notifications")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await notificationsPeeker.Peek(peekRequest);
            return new JsonResult(response);
        }
    }
}