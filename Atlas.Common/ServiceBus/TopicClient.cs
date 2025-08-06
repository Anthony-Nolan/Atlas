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

    public sealed class TopicClient : ITopicClient, IAsyncDisposable
    {
        private readonly ServiceBusSender serviceBusSender;

        public TopicClient(ServiceBusSender serviceBusSender)
        {
            this.serviceBusSender = serviceBusSender;
        }

        public ValueTask DisposeAsync() => serviceBusSender.DisposeAsync();

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
