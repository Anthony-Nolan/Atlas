using Atlas.Common.ServiceBus;

namespace Atlas.ManualTesting.Common.Services
{
    public abstract class MessageSender<TMessage> where TMessage : class, new()
    {
        private readonly IMessageBatchPublisher<TMessage> messagePublisher;
        private readonly string resultsBlobContainer;

        protected MessageSender(
            IMessageBatchPublisher<TMessage> messagePublisher,
            string resultsBlobContainerName)
        {
            this.messagePublisher = messagePublisher;
            resultsBlobContainer = resultsBlobContainerName;
        }

        protected async Task BuildAndSendMessages(IEnumerable<string> requestIds)
        {
            var messages = requestIds.Select(id => BuildMessage(id, resultsBlobContainer));
            await messagePublisher.BatchPublish(messages);
        }

        protected abstract TMessage BuildMessage(string requestId, string resultsBlobContainerName);
    }
}