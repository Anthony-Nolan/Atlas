using Azure.Messaging.ServiceBus;
using System;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus
{
    public interface ITopicClient: IAsyncDisposable
    {
        Task SendAsync(ServiceBusMessage message);
        Task SendBatchAsync(ServiceBusMessageBatch messages);
        Task <ServiceBusMessageBatch> CreateMessageBatchAsync();
    }

    /// <summary>
    /// The client of one topic. <see cref="TopicClientFactory"/> builds one for each topic, and all callers of the topic share it.
    /// </summary>
    internal sealed class TopicClient : ITopicClient, IAsyncDisposable
    {
        private readonly ServiceBusSender serviceBusSender;

        public TopicClient(ServiceBusSender serviceBusSender)
        {
            this.serviceBusSender = serviceBusSender;
        }

        /// <summary>
        /// Does nothing: other callers share this client, and <see cref="TopicClientFactory"/> closes its sender when the factory is disposed.
        /// </summary>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <summary>Closes the sender, and so its link. Only <see cref="TopicClientFactory"/> calls this.</summary>
        internal ValueTask CloseAsync() => serviceBusSender.DisposeAsync();

        public async Task SendAsync(ServiceBusMessage message)
        {
            await serviceBusSender.SendMessageAsync(message);
        }

        public async Task SendBatchAsync(ServiceBusMessageBatch messages)
        {
            await serviceBusSender.SendMessagesAsync(messages);
        }

        public async Task<ServiceBusMessageBatch> CreateMessageBatchAsync()
        {
            return await serviceBusSender.CreateMessageBatchAsync();
        }
    }
}
