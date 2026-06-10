using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Validation;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SearchInitiationResponse = Atlas.MatchingAlgorithm.Client.Models.SearchRequests.SearchInitiationResponse;

namespace Atlas.MatchingAlgorithm.Functions.Functions;

public class SearchFunctions
{
    private readonly ISearchDispatcher searchDispatcher;
    private readonly ISearchRunner searchRunner;
    private readonly IMatchingFailureNotificationSender matchingFailureNotificationSender;
    private readonly ILogger<SearchFunctions> logger;
    private readonly MatchingAlgorithmSearchLoggingContext searchLoggingContext;

    public SearchFunctions(
        ISearchDispatcher searchDispatcher,
        ISearchRunner searchRunner,
        IMatchingFailureNotificationSender matchingFailureNotificationSender,
        ILogger<SearchFunctions> logger,
        MatchingAlgorithmSearchLoggingContext searchLoggingContext)
    {
        this.searchDispatcher = searchDispatcher;
        this.searchRunner = searchRunner;
        this.matchingFailureNotificationSender = matchingFailureNotificationSender;
        this.logger = logger;
        this.searchLoggingContext = searchLoggingContext;
    }

    [Function(nameof(InitiateSearch))]
    public async Task<IActionResult> InitiateSearch(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequest request)
    {
        var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
        try
        {
            var id = await searchDispatcher.DispatchSearch(searchRequest);
            return new JsonResult(new SearchInitiationResponse { SearchIdentifier = id });
        }
        catch (ValidationException e)
        {
            return new BadRequestObjectResult(e.ToValidationErrorsModel());
        }
    }

    [Function(nameof(RunSearch))]
    public async Task RunSearch(
        [ServiceBusTrigger(
            "%MessagingServiceBus:SearchRequestsTopic%",
            "%MessagingServiceBus:SearchRequestsSubscription%",
            Connection = "MessagingServiceBus:ConnectionString"
        )]
        IdentifiedSearchRequest request,
        int deliveryCount,
        DateTime enqueuedTimeUtc,
        CancellationToken cancellationToken)
    {
        searchLoggingContext.SearchRequestId = request.Id;
        try
        {
            logger.LogInformation("Function {FunctionName} executing; Search Id: {SearchId}", nameof(RunSearch), request.Id);

            enqueuedTimeUtc = DateTime.SpecifyKind(enqueuedTimeUtc, DateTimeKind.Utc);
            await searchRunner.RunSearch(request, deliveryCount, enqueuedTimeUtc).WaitAsync(cancellationToken);

            logger.LogInformation("Function {FunctionName} executed; Search Id: {SearchId}", nameof(RunSearch), request.Id);
        }
        catch (OperationCanceledException ex)
        {
            var wrappedException = new OperationCanceledException($"Function {nameof(RunSearch)} has been cancelled; " + $"Search Id: {request.Id}", ex, cancellationToken);

            logger.LogError(wrappedException, "Function {FunctionName} has been cancelled; Search Id: {SearchId}", nameof(RunSearch), request.Id);

            throw wrappedException;
        }
    }

    [Function(nameof(MatchingRequestsDeadLetterQueueListener))]
    public async Task MatchingRequestsDeadLetterQueueListener(
        [ServiceBusTrigger(
            "%MessagingServiceBus:SearchRequestsTopic%/Subscriptions/%MessagingServiceBus:SearchRequestsSubscription%/$DeadLetterQueue",
            "%MessagingServiceBus:SearchRequestsSubscription%",
            Connection = "MessagingServiceBus:ConnectionString"
        )]
        IdentifiedSearchRequest request,
        int deliveryCount)
    {
        await matchingFailureNotificationSender.SendFailureNotification(request, deliveryCount, 0);
    }
}