using Atlas.Common.ServiceBus.BatchReceiving;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.Common.Test.ServiceBus.BatchReceiving
{
    [TestFixture]
    public class MessageProcessorTests
    {
        private IServiceBusMessageReceiver<string> messageReceiver;
        private IMessageProcessor<string> messageProcessor;

        [SetUp]
        public void SetUp()
        {
            messageReceiver = Substitute.For<IServiceBusMessageReceiver<string>>();
            messageProcessor = new MessageProcessor<string>(messageReceiver);
        }

        [Test]
        public async Task ProcessAllMessagesInBatches_ReceivesBatchOfMessages()
        {
            const int batchSize = 123;

            await messageProcessor.ProcessAllMessagesInBatches_Async(messages => Task.CompletedTask, batchSize);

            await messageReceiver.Received(1).ReceiveMessageBatchAsync(batchSize);
        }
    }
}
