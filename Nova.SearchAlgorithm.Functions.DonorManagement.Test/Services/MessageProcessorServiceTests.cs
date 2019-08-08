using Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Test.Services
{
    [TestFixture]
    public class MessageProcessorServiceTests
    {
        private const string TopicName = "topic";
        private const string SubscriptionName = "subscription";

        private IMessageReceiverFactory factory;
        private IMessageProcessorService<string> messageProcessorService;

        [SetUp]
        public void SetUp()
        {
            factory = Substitute.For<IMessageReceiverFactory>();
            messageProcessorService = new MessageProcessorService<string>(factory, TopicName, SubscriptionName);
        }

        [Test]
        public void GetsMessageReceiver()
        {
            factory.Received(1).GetMessageReceiver(TopicName, SubscriptionName);
        }
    }
}
