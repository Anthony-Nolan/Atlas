using System.Text;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Config;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Atlas.Common.Utils;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;

namespace Atlas.SearchTracking.Common.Clients;

public interface ISearchTrackingServiceBusClient
{
    Task PublishSearchTrackingEvent<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType) where TEvent : ISearchTrackingEvent;
}

public class SearchTrackingServiceBusClient : ISearchTrackingServiceBusClient
{
    // Key used to resolve the shared, singleton ServiceBusSender registered in RegisterSearchTrackingServiceBusClient.
    public const string ServiceBusKey = "SearchTracking";

    private readonly ServiceBusSender sender;
    private readonly int sendRetryCount;
    private readonly int sendRetryCooldownSeconds;
    private readonly IAtlasLogger logger;

    public SearchTrackingServiceBusClient(
        [FromKeyedServices(ServiceBusKey)] ServiceBusSender sender,
        SearchTrackingServiceBusSettings searchTrackingServiceBusSettings,
        IAtlasLogger logger)
    {
        // The sender (and its underlying ServiceBusClient/AMQP connection) is a shared singleton - thread-safe and
        // reused for the app lifetime, per Azure SDK guidance. This client must NOT create or dispose its own.
        this.sender = sender;
        sendRetryCount = searchTrackingServiceBusSettings.SendRetryCount;
        sendRetryCooldownSeconds = searchTrackingServiceBusSettings.SendRetryCooldownSeconds;
        this.logger = logger;
    }

    public async Task PublishSearchTrackingEvent<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType) where TEvent : ISearchTrackingEvent
    {
        var json = JsonConvert.SerializeObject(searchTrackingEvent);
        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

        message.ApplicationProperties[SearchTrackingConstants.EventType] = eventType.ToString();
        message.ApplicationProperties.Add("SearchIdentifier", searchTrackingEvent.SearchIdentifier);
        message.ApplicationProperties.Add("OriginalSearchIdentifier", searchTrackingEvent.OriginalSearchIdentifier);
        message.SessionId = searchTrackingEvent.SearchIdentifier.ToString();
        if (searchTrackingEvent is ISearchTrackingMatchingAttemptEvent attemptEvent)
        {
            message.ApplicationProperties["AttemptNumber"] = attemptEvent.AttemptNumber;
        }

        var retryPolicy = Policy
            .Handle<ServiceBusException>()
            .WaitAndRetryAsync(
                sendRetryCount,
                _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds),
                onRetry: (exception, timespan, attemptNumber, context) =>
                    logger.SendTrace($"Could not send search tracking event message to Service Bus; attempt {attemptNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));

        using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
        {
            await retryPolicy.ExecuteAsync(async () => await sender.SendMessageAsync(message));
        }
    }
}