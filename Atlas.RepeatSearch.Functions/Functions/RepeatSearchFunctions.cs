using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Utils;
using Atlas.Common.Validation;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.Search;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class RepeatSearchFunctions
    {
        private readonly IRepeatSearchDispatcher repeatSearchDispatcher;
        private readonly IRepeatSearchRunner repeatSearchRunner;
        private readonly IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender;

        public RepeatSearchFunctions(IRepeatSearchDispatcher repeatSearchDispatcher, IRepeatSearchRunner repeatSearchRunner, IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender)
        {
            this.repeatSearchDispatcher = repeatSearchDispatcher;
            this.repeatSearchRunner = repeatSearchRunner;
            this.repeatSearchMatchingFailureNotificationSender = repeatSearchMatchingFailureNotificationSender;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(InitiateRepeatSearch))]
        public async Task<IActionResult> InitiateRepeatSearch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(RepeatSearchRequest), nameof(RepeatSearchRequest))]
            HttpRequest request)
        {
            var repeatSearchRequest = JsonConvert.DeserializeObject<RepeatSearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            try
            {
                var id = await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);
                return new JsonResult(new SearchInitiationResponse { SearchIdentifier = id });
            }
            catch (ValidationException e)
            {
                return new BadRequestObjectResult(e.ToValidationErrorsModel());
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RunRepeatSearch))]
        public async Task RunRepeatSearch(
            [ServiceBusTrigger(
                "%MessagingServiceBus:RepeatSearchRequestsTopic%",
                "%MessagingServiceBus:RepeatSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            IdentifiedRepeatSearchRequest request,
            int deliveryCount)
        {
            await repeatSearchRunner.RunSearch(request, deliveryCount);
        }

        [Function(nameof(RepeatSearchMatchingRequestsDeadLetterQueueListener))]
        public async Task RepeatSearchMatchingRequestsDeadLetterQueueListener(
            [ServiceBusTrigger(
                "%MessagingServiceBus:RepeatSearchRequestsTopic%/Subscriptions/%MessagingServiceBus:RepeatSearchRequestsSubscription%/$DeadLetterQueue",
                "%MessagingServiceBus:RepeatSearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            IdentifiedRepeatSearchRequest request,
            int deliveryCount)
        {
            await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(request, deliveryCount, 0);
        }
    }
}
