using Atlas.Client.Models.Debug;
using Atlas.Common.Utils.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ServiceBus;

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

        [FunctionName(nameof(PeekAlerts))]
        [ProducesResponseType(typeof(IEnumerable<Alert>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekAlerts(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/alerts/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var messages = await alertsPeeker.Peek(peekRequest);
            return new JsonResult(messages.Select(m => m.DeserializedBody));
        }

        [FunctionName(nameof(PeekNotifications))]
        [ProducesResponseType(typeof(IEnumerable<Notification>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> PeekNotifications(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = $"{RouteConstants.DebugRoutePrefix}/notifications/")]
            [RequestBodyType(typeof(PeekServiceBusMessagesRequest), nameof(PeekServiceBusMessagesRequest))]
            HttpRequest request)
        {
            var peekRequest = await request.DeserialiseRequestBody<PeekServiceBusMessagesRequest>();
            var messages = await notificationsPeeker.Peek(peekRequest);
            return new JsonResult(messages.Select(m => m.DeserializedBody));
        }
    }
}