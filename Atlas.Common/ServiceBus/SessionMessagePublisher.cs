using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

    public SessionMessagePublisher(
        ITopicClientFactory topicClientFactory,
        string topicName,
        int sendRetryCount,
        int sendRetryCooldownSeconds,
        ILogger<SessionMessagePublisher<T>> logger)
    {
        this.topicClient = topicClientFactory.BuildTopicClient(topicName);
        this.sendRetryCount = sendRetryCount;
        this.sendRetryCooldownSeconds = sendRetryCooldownSeconds;
        this.logger = logger;
    }

    public async Task PublishWithSession(T content, string sessionId)
    {
        var json = JsonConvert.SerializeObject(content);
        var message = new ServiceBusMessage(json)
        {
            SessionId = sessionId
        };

        await topicClient.SendWithRetryAndWaitAsync(
            message,
            sendRetryCount,
            sendRetryCooldownSeconds,
            (exception, retryNumber) => logger.LogWarning(
                "Could not send session message to Service Bus; attempt {RetryNumber}/{MaxRetries}; exception: {Exception}",
                retryNumber, sendRetryCount, exception));
    }
}