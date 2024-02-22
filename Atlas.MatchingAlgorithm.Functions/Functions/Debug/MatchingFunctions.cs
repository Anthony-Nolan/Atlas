using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ServiceBus;
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

        public MatchingFunctions(IServiceBusPeeker<MatchingResultsNotification> notificationsPeeker)
        {
            this.notificationsPeeker = notificationsPeeker;
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
    }
}