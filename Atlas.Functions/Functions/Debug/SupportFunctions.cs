using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Debugging;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ServiceBus;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.Functions.Functions.Debug
{
    public class SupportFunctions
    {
        private readonly IServiceBusPeeker<Alert> alertsPeeker;
        private readonly IServiceBusPeeker<Notification> notificationsPeeker;

        public SupportFunctions(IServiceBusPeeker<Alert> alertsPeeker, IServiceBusPeeker<Notification> notificationsPeeker)
        {
            this.alertsPeeker = alertsPeeker;
            this.notificationsPeeker = notificationsPeeker;
        }

        [Function(nameof(PeekAlerts))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<Alert>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekAlerts(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/alerts/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await alertsPeeker.Peek(peekRequest);
            return new JsonResult(response);
        }

        [Function(nameof(PeekNotifications))]
        [ProducesResponseType(typeof(PeekServiceBusMessagesResponse<Notification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekNotifications(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/notifications/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var response = await notificationsPeeker.Peek(peekRequest);
            return new JsonResult(response);
        }
    }
}