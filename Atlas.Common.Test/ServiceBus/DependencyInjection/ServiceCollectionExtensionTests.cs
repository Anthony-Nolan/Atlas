using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.DependencyInjection;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.ServiceBus.DependencyInjection;

[TestFixture]
public class ServiceCollectionExtensionTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public async Task RegisterServiceBusAsKeyedServices_ForTwoKeys_GivesEachKeyItsOwnClients()
    {
        var topicName = fixture.Create<string>();
        var serviceBusClients = fixture.CreateMany<string>(2).ToDictionary(key => key, _ => SubstituteServiceBusClient());
        var services = new ServiceCollection();
        foreach (var (key, serviceBusClient) in serviceBusClients)
        {
            services.RegisterServiceBusAsKeyedServices(key, _ => fixture.Create<string>());

            // The last registration of a key wins, so the factory of the key gets this client.
            services.AddKeyedSingleton(key, serviceBusClient);
        }
        await using var provider = services.BuildServiceProvider();
        var factories = serviceBusClients.Keys.Select(key => provider.GetRequiredKeyedService<ITopicClientFactory>(key)).ToList();

        var firstKeyClient = factories[0].BuildTopicClient(topicName);
        var secondKeyClient = factories[1].BuildTopicClient(topicName);

        secondKeyClient.Should().NotBeSameAs(firstKeyClient);
        foreach (var serviceBusClient in serviceBusClients.Values)
        {
            serviceBusClient.Received(1).CreateSender(topicName);
        }
    }

    // The provider disposes the factories at the end of the test, and they close the senders that they built.
    private static ServiceBusClient SubstituteServiceBusClient()
    {
        var serviceBusClient = Substitute.For<ServiceBusClient>();
        serviceBusClient.CreateSender(Arg.Any<string>()).Returns(_ => Substitute.For<ServiceBusSender>());
        return serviceBusClient;
    }
}
