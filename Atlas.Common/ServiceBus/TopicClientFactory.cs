using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Atlas.Common.ServiceBus;

public interface ITopicClientFactory
{
    /// <summary>
    /// Gives the client of the topic. All callers get the same client for a topic, so they share one sender and its link.
    /// The factory owns the sender and closes it when the factory is disposed: disposing the client does nothing.
    /// </summary>
    ITopicClient BuildTopicClient(string topicName);
}

internal sealed class TopicClientFactory : ITopicClientFactory, IAsyncDisposable
{
    private readonly ServiceBusClient client;

    // Lazy, so that callers that ask for a new topic at the same time make one sender, not one each.
    // Lazy also keeps an exception from CreateSender. That changes nothing: CreateSender only checks the topic name and that the
    // client is open, and the link opens on the first send, so it fails the same way on every call.
    private readonly ConcurrentDictionary<string, Lazy<TopicClient>> topicClients = new();

    public TopicClientFactory(ServiceBusClient client)
    {
        this.client = client;
    }

    /// <inheritdoc />
    public ITopicClient BuildTopicClient(string topicName) =>
        topicClients.GetOrAdd(topicName, name => new Lazy<TopicClient>(() => new TopicClient(client.CreateSender(name)))).Value;

    /// <summary>Closes the senders of all the clients that the factory built.</summary>
    public async ValueTask DisposeAsync() =>
        await Task.WhenAll(topicClients.Values.Where(c => c.IsValueCreated).Select(c => c.Value.CloseAsync().AsTask()));
}
