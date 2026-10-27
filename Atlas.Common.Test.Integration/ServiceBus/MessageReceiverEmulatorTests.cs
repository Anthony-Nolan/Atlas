using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NUnit.Framework;

namespace Atlas.Common.Test.Integration.ServiceBus
{
    /// <summary>
    /// Runs against the Azure Service Bus emulator. Its own build pipeline job starts the emulator and sets
    /// RUN_SERVICE_BUS_EMULATOR_TESTS; without that variable the fixture ignores itself.
    /// </summary>
    [TestFixture]
    public class MessageReceiverEmulatorTests
    {
        private const string EmulatorVariable = "RUN_SERVICE_BUS_EMULATOR_TESTS";
        private const string ConnectionString =
            "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        private const string AdminConnectionString =
            "Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        private const string Subscription = "receiver-check";

        // Long enough that no lock expires while a test waits, so a message can only come back by being released.
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ReceiveWait = TimeSpan.FromSeconds(10);
        // How long another receiver must get nothing for the messages to count as held by a link. The emulator shows no
        // lock state otherwise: its peek and runtime properties do not tell locked messages from available ones.
        private static readonly TimeSpan HeldCheckWait = TimeSpan.FromSeconds(3);

        private ServiceBusAdministrationClient admin;
        private ServiceBusClient client;
        private string topic;

        [SetUp]
        public async Task SetUp()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EmulatorVariable)))
            {
                Assert.Ignore($"Set {EmulatorVariable} and start the Service Bus emulator. See README_DevelopmentStartUpGuide.md.");
            }

            topic = "receiver-" + Guid.NewGuid().ToString("N");
            admin = new ServiceBusAdministrationClient(AdminConnectionString);
            await admin.CreateTopicAsync(topic);
            try
            {
                await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(topic, Subscription) { LockDuration = LockDuration });
            }
            catch
            {
                // TearDown does not run when SetUp fails.
                await admin.DeleteTopicAsync(topic);
                throw;
            }

            client = new ServiceBusClient(ConnectionString);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (client != null)
            {
                await client.DisposeAsync();
                client = null;
                await admin.DeleteTopicAsync(topic);
            }
        }

        [Test]
        public async Task CreateMessageReceiver_AfterEarlierReceiverWasDisposed_GivesWorkingReceiver()
        {
            await using var sender = client.CreateSender(topic);
            await sender.SendMessageAsync(new ServiceBusMessage("one"));

            var factory = new MessageReceiverFactory(client);
            var receiver = factory.CreateMessageReceiver(topic, Subscription);
            var firstBatch = (await receiver.ReceiveMessagesAsync(1)).ToList();
            firstBatch.Should().ContainSingle();
            await receiver.CompleteMessageAsync(firstBatch[0]);

            await receiver.DisposeAsync();

            var receiveAfterDispose = async () => await receiver.ReceiveMessagesAsync(1);
            await receiveAfterDispose.Should().ThrowAsync<ObjectDisposedException>();

            await sender.SendMessageAsync(new ServiceBusMessage("two"));
            await using var nextReceiver = factory.CreateMessageReceiver(topic, Subscription);
            var secondBatch = (await nextReceiver.ReceiveMessagesAsync(1)).ToList();
            secondBatch.Should().ContainSingle();
            secondBatch[0].Body.ToString().Should().Be("two");
            await nextReceiver.CompleteMessageAsync(secondBatch[0]);
        }

        /// <summary>
        /// Without prefetch a link holds no messages between receives: the broker can still send it messages for the credit
        /// left over after a receive, but the client releases each one at once, because no receive is waiting for it.
        /// So the processor here prefetches: the messages it has not processed are locked on its link, and only closing
        /// that link gives them back to the subscription. Before the processor is disposed, the test checks that another
        /// receiver cannot get them, so the messages the next receiver gets afterwards were released, not just late.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public async Task DisposeAsync_ReleasesMessagesPrefetchedOnTheLink(bool disposeProcessor)
        {
            await using var sender = client.CreateSender(topic);
            foreach (var body in new[] { "\"m0\"", "\"m1\"", "\"m2\"" })
            {
                await sender.SendMessageAsync(new ServiceBusMessage(body));
            }

            var processor = new MessageProcessor<string>(
                new ServiceBusMessageReceiver<string>(new PrefetchingMessageReceiverFactory(client), topic, Subscription));
            var processed = new List<string>();
            await processor.ProcessAllMessagesInBatches_Async(
                batch =>
                {
                    processed.AddRange(batch.Select(m => m.DeserializedBody));
                    return Task.CompletedTask;
                },
                batchSize: 1);
            processed.Should().Equal("m0");
            var processorDisposed = false;

            try
            {
                // Closed before the processor, so no credit is left on its link to take the released messages.
                await using (var heldCheckReceiver = client.CreateReceiver(topic, Subscription))
                {
                    var notHeld = await ReceiveUpTo(heldCheckReceiver, 2, HeldCheckWait);
                    notHeld.Should().BeEmpty("m1 and m2 should be locked on the processor's link before it is disposed");
                }

                if (disposeProcessor)
                {
                    await processor.DisposeAsync();
                    processorDisposed = true;
                }

                await using var nextReceiver = client.CreateReceiver(topic, Subscription);
                var received = await ReceiveUpTo(nextReceiver, 2, ReceiveWait);

                if (disposeProcessor)
                {
                    received.Select(m => m.Body.ToString()).Should().BeEquivalentTo("\"m1\"", "\"m2\"");
                    // Released, not abandoned: the next receiver is the first to get them.
                    received.Should().OnlyContain(m => m.DeliveryCount == 1);
                }
                else
                {
                    received.Should().BeEmpty();
                }
            }
            finally
            {
                if (!processorDisposed)
                {
                    await processor.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// The ATL-482 scenario, with the production factory: in the QueuedDonorUpdateProcessing stage the Data Refresh app
        /// received once, the receive returned before the batch was full and left link credit, and the receiver stayed open
        /// for the life of the process. The broker sends that link the messages that arrive later, but the client
        /// releases them at once, because no receive is waiting. If an SDK upgrade made the link keep them instead, they would
        /// stay locked until the lock expires, as with prefetch, and this test would fail.
        /// </summary>
        [Test]
        public async Task ProcessAllMessagesInBatches_WithoutPrefetch_LeavesLaterMessagesFreeForOtherReceivers()
        {
            await using var sender = client.CreateSender(topic);
            await sender.SendMessageAsync(new ServiceBusMessage("\"m0\""));

            await using var processor = new MessageProcessor<string>(
                new ServiceBusMessageReceiver<string>(new MessageReceiverFactory(client), topic, Subscription));
            var processed = new List<string>();
            await processor.ProcessAllMessagesInBatches_Async(
                batch =>
                {
                    processed.AddRange(batch.Select(m => m.DeserializedBody));
                    return Task.CompletedTask;
                },
                batchSize: 10);
            processed.Should().Equal("m0");

            await sender.SendMessageAsync(new ServiceBusMessage("\"m1\""));
            await sender.SendMessageAsync(new ServiceBusMessage("\"m2\""));

            await using var nextReceiver = client.CreateReceiver(topic, Subscription);
            var received = await ReceiveUpTo(nextReceiver, 2, ReceiveWait);

            received.Select(m => m.Body.ToString()).Should().BeEquivalentTo("\"m1\"", "\"m2\"");
            // Released, never locked or abandoned.
            received.Should().OnlyContain(m => m.DeliveryCount == 1);
        }

        private static async Task<List<ServiceBusReceivedMessage>> ReceiveUpTo(ServiceBusReceiver receiver, int count, TimeSpan wait)
        {
            var received = new List<ServiceBusReceivedMessage>();
            var deadline = DateTime.UtcNow + wait;

            while (received.Count < count)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                received.AddRange(await receiver.ReceiveMessagesAsync(count - received.Count, remaining));
            }

            return received;
        }

        private class PrefetchingMessageReceiverFactory : IMessageReceiverFactory
        {
            private readonly ServiceBusClient client;

            public PrefetchingMessageReceiverFactory(ServiceBusClient client)
            {
                this.client = client;
            }

            public IMessageReceiver CreateMessageReceiver(string topicName, string subscriptionName) =>
                new MessageReceiver(client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions { PrefetchCount = 10 }));
        }
    }
}
