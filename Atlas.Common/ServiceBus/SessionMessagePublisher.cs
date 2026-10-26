using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus;

public interface ISessionMessagePublisher<in T>
{
    /// <summary>Serialises <paramref name="content"/> and sends it as a single Service Bus message with the given <paramref name="sessionId"/>.</summary>
    Task PublishWithSession(T content, string sessionId);
}

public class SessionMessagePublisher<T> : ISessionMessagePublisher<T>
{
    private readonly ITopicClient topicClient;
    private readonly int sendRetryCount;
    private readonly int sendRetryCooldownSeconds;
    private readonly ILogger<SessionMessagePublisher<T>> logger;
    private readonly Func<T, IReadOnlyDictionary<string, object>> applicationPropertiesSelector;

    /// <param name="applicationPropertiesSelector">
    /// Optional. Selects the values to stamp as Service Bus <see cref="ServiceBusMessage.ApplicationProperties"/> on each message,
    /// so they are visible without reading the body. See <see cref="ServiceBusMessageExtensions.AddApplicationProperties"/>.
    /// </param>
    public SessionMessagePublisher(
        ITopicClientFactory topicClientFactory,
        string topicName,
        int sendRetryCount,
        int sendRetryCooldownSeconds,
        ILogger<SessionMessagePublisher<T>> logger,
        Func<T, IReadOnlyDictionary<string, object>> applicationPropertiesSelector = null)
    {
        this.topicClient = topicClientFactory.BuildTopicClient(topicName);
        this.sendRetryCount = sendRetryCount;
        this.sendRetryCooldownSeconds = sendRetryCooldownSeconds;
        this.logger = logger;
        this.applicationPropertiesSelector = applicationPropertiesSelector;
    }

    public async Task PublishWithSession(T content, string sessionId)
    {
        var json = JsonConvert.SerializeObject(content);
        var message = new ServiceBusMessage(json)
        {
            SessionId = sessionId
        };
        message.AddApplicationProperties(applicationPropertiesSelector?.Invoke(content));

        await topicClient.SendWithRetryAndWaitAsync(
            message,
            sendRetryCount,
            sendRetryCooldownSeconds,
            (exception, retryNumber) => logger.LogWarning(
                "Could not send session message to Service Bus; attempt {RetryNumber}/{MaxRetries}; exception: {Exception}",
                retryNumber, sendRetryCount, exception));
    }
}