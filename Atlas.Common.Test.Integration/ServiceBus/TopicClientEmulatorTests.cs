using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.Integration.ServiceBus;

/// <summary>
/// Checks the send links of the publishers against the Azure Service Bus emulator (see <see cref="ServiceBusEmulator"/>).
/// The publishers are registered as the apps register them, and each one is used in its own async scope, as the match
/// prediction worker does for each batch message. A test counts the open send links of its topic with
/// <see cref="SendLinkCounter"/>.
/// </summary>
[TestFixture]
public class TopicClientEmulatorTests
{
    private const string ServiceKey = "emulator";
    private const int Scopes = 20;
    private const int PerMessageClients = 20;
    private const int SendRetryCount = 1;
    private const int SendRetryCooldownSeconds = 0;

    // How long no link may close for the open links to count as kept open.
    private static readonly TimeSpan OpenLinkCheckWait = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LinkCloseWait = TimeSpan.FromSeconds(10);

    private Fixture fixture;
    private ServiceBusAdministrationClient admin;
    private ServiceBusClient client;
    private string topic;

    [SetUp]
    public async Task SetUp()
    {
        ServiceBusEmulator.IgnoreUnlessEnabled();

        fixture = new Fixture();
        topic = "sender-" + Guid.NewGuid().ToString("N");
        admin = new ServiceBusAdministrationClient(ServiceBusEmulator.AdminConnectionString);
        await admin.CreateTopicAsync(topic);

        client = new ServiceBusClient(ServiceBusEmulator.ConnectionString);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (client != null)
        {
            if (!client.IsClosed)
            {
                await client.DisposeAsync();
            }

            client = null;
            await admin.DeleteTopicAsync(topic);
        }
    }

    /// <summary>
    /// The publishers before ATL-208: the factory made a new sender for each call, and the publishers that live in a scope
    /// did not close their sender. The end of the scope and garbage collection do not close the link of such a sender: the
    /// connection keeps each open link until the link closes. Only closing the connection closes them here. Azure Service
    /// Bus also detaches a send link after 10 minutes without sends; the test does not wait that long.
    /// </summary>
    [Test]
    public async Task ScopedPublishers_WithSenderPerCall_LeaveEverySenderLinkOpen()
    {
        using var links = new SendLinkCounter(topic);
        await using var provider = BuildProvider(services =>
            services.AddKeyedSingleton<ITopicClientFactory>(ServiceKey, new SenderPerCallTopicClientFactory(client)));

        await PublishInScopes(provider);
        await CollectGarbageAndWait();

        links.Open.Should().Be(2 * Scopes, "the two publishers of each scope opened a send link, and nothing closed it");

        await client.DisposeAsync();
        await WaitUntil(() => links.Open == 0);

        links.Open.Should().Be(0, "closing the connection closes its links, and the counter sees them close");
    }

    /// <summary>
    /// The publishers with <see cref="TopicClientFactory"/>: all the publishers of the topic share one sender, and so one
    /// link. The callers that build, use and dispose a client for each message, as SearchServiceBusClient does, share it
    /// too: disposing their client does not close the sender, else the sends after it would fail.
    /// </summary>
    [Test]
    public async Task ScopedPublishers_WithTopicClientFactory_ShareOneLinkPerTopic()
    {
        using var links = new SendLinkCounter(topic);
        await using var provider = BuildProvider(RegisterTopicClientFactory);

        await PublishInScopes(provider);
        await SendWithClientPerMessage(provider);
        await CollectGarbageAndWait();

        links.Open.Should().Be(1, "all the publishers and callers of the topic share one sender, and nothing closed it");
    }

    /// <summary>
    /// At host shutdown, the service provider disposes <see cref="TopicClientFactory"/>, and the factory closes the senders
    /// that it built. The provider does not dispose the <see cref="ServiceBusClient"/> of the test, so only the factory can
    /// close the link here.
    /// </summary>
    [Test]
    public async Task DisposeAsync_OnServiceProvider_ClosesTheFactorysLinks()
    {
        using var links = new SendLinkCounter(topic);
        var provider = BuildProvider(RegisterTopicClientFactory);
        await provider.GetRequiredKeyedService<ITopicClientFactory>(ServiceKey).BuildTopicClient(topic)
            .SendAsync(new ServiceBusMessage(fixture.Create<string>()));
        links.Open.Should().Be(1, "the send opened the link of the factory's sender");

        await provider.DisposeAsync();
        await WaitUntil(() => links.Open == 0);

        links.Open.Should().Be(0, "disposing the provider disposes the factory, which closes its sender");
        client.IsClosed.Should().BeFalse("the factory closes its senders, not the connection");
    }

    private void RegisterTopicClientFactory(IServiceCollection services)
    {
        services.RegisterServiceBusAsKeyedServices(ServiceKey, _ => ServiceBusEmulator.ConnectionString);

        // The last registration of a key wins, so the factory gets the client of the test. The provider does not dispose a
        // client that it did not build.
        services.AddKeyedSingleton(ServiceKey, client);
    }

    private ServiceProvider BuildProvider(Action<IServiceCollection> registerTopicClientFactory)
    {
        var services = new ServiceCollection();
        registerTopicClientFactory(services);

        services.AddScoped<ISessionMessagePublisher<string>>(sp => new SessionMessagePublisher<string>(
            sp.GetRequiredKeyedService<ITopicClientFactory>(ServiceKey),
            topic,
            SendRetryCount,
            SendRetryCooldownSeconds,
            NullLogger<SessionMessagePublisher<string>>.Instance));

        services.AddScoped<IMessageBatchPublisher<string>>(sp => new MessageBatchPublisher<string>(
            sp.GetRequiredKeyedService<ITopicClientFactory>(ServiceKey),
            topic,
            SendRetryCount,
            SendRetryCooldownSeconds,
            Substitute.For<IAtlasLogger>()));

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private async Task PublishInScopes(IServiceProvider provider)
    {
        for (var i = 0; i < Scopes; i++)
        {
            await using var scope = provider.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<ISessionMessagePublisher<string>>()
                .PublishWithSession(fixture.Create<string>(), fixture.Create<string>());
            await scope.ServiceProvider.GetRequiredService<IMessageBatchPublisher<string>>()
                .BatchPublish(fixture.CreateMany<string>());
        }
    }

    /// <summary>
    /// Sends with a client for each message, as SearchServiceBusClient does: each caller builds a client, sends one message
    /// and disposes the client.
    /// </summary>
    private async Task SendWithClientPerMessage(IServiceProvider provider)
    {
        var factory = provider.GetRequiredKeyedService<ITopicClientFactory>(ServiceKey);

        for (var i = 0; i < PerMessageClients; i++)
        {
            await using var topicClient = factory.BuildTopicClient(topic);
            await topicClient.SendAsync(new ServiceBusMessage(fixture.Create<string>()));
        }
    }

    /// <summary>
    /// Collects the publishers and senders that nothing refers to any more, then waits, so that a link that closes has time
    /// to report it.
    /// </summary>
    private static async Task CollectGarbageAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Task.Delay(OpenLinkCheckWait);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + LinkCloseWait;

        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    /// <summary>The factory before ATL-208: a new sender, and so a new link, for each call.</summary>
    private sealed class SenderPerCallTopicClientFactory(ServiceBusClient client) : ITopicClientFactory
    {
        public ITopicClient BuildTopicClient(string topicName) => new TopicClient(client.CreateSender(topicName));
    }
}
