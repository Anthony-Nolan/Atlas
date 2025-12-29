using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Utils;
using Atlas.Common.Validation;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SearchInitiationResponse = Atlas.MatchingAlgorithm.Client.Models.SearchRequests.SearchInitiationResponse;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class SearchFunctions
    {
        private readonly ISearchDispatcher searchDispatcher;
        private readonly ISearchRunner searchRunner;
        private readonly IMatchingFailureNotificationSender matchingFailureNotificationSender;
        private readonly ILogger<SearchFunctions> logger;

        public SearchFunctions(ISearchDispatcher searchDispatcher, ISearchRunner searchRunner, IMatchingFailureNotificationSender matchingFailureNotificationSender, ILogger<SearchFunctions> logger)
        {
            this.searchDispatcher = searchDispatcher;
            this.searchRunner = searchRunner;
            this.matchingFailureNotificationSender = matchingFailureNotificationSender;
            this.logger = logger;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(InitiateSearch))]
        public async Task<IActionResult> InitiateSearch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            try
            {
                var id = await searchDispatcher.DispatchSearch(searchRequest);
                return new JsonResult(new SearchInitiationResponse {SearchIdentifier = id});
            }
            catch (ValidationException e)
            {
                return new BadRequestObjectResult(e.ToValidationErrorsModel());
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RunSearch))]
        public async Task RunSearch(
            [ServiceBusTrigger(
                "%MessagingServiceBus:SearchRequestsTopic%",
                "%MessagingServiceBus:SearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            IdentifiedSearchRequest request,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Function {FunctionName} executing", nameof(RunSearch));

                enqueuedTimeUtc = DateTime.SpecifyKind(enqueuedTimeUtc, DateTimeKind.Utc);
                await searchRunner.RunSearch(request, deliveryCount, enqueuedTimeUtc).WaitAsync(cancellationToken);

                logger.LogInformation("Function {FunctionName} executed", nameof(RunSearch));
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Function {FunctionName} has been cancelled; Search Id: {SearchId}", nameof(RunSearch), request.Id);
                throw;
            }
        }

        [Function(nameof(MatchingRequestsDeadLetterQueueListener))]
        public async Task MatchingRequestsDeadLetterQueueListener(
            [ServiceBusTrigger(
                "%MessagingServiceBus:SearchRequestsTopic%/Subscriptions/%MessagingServiceBus:SearchRequestsSubscription%/$DeadLetterQueue",
                "%MessagingServiceBus:SearchRequestsSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            IdentifiedSearchRequest request,
            int deliveryCount)
        {
            await matchingFailureNotificationSender.SendFailureNotification(request, deliveryCount, 0);
        }
    }
}