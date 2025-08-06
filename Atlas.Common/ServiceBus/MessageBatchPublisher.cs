using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageBatchPublisher<in T>
    {
        Task BatchPublish(IEnumerable<T> contentToPublish);
    }

    public class MessageBatchPublisher<T> : IMessageBatchPublisher<T>
    {
        private readonly ITopicClient topicClient;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public MessageBatchPublisher(ITopicClientFactory topicClientFactory, string topicName, int sendRetryCount, int sendRetryCooldownSeconds, ILogger logger)
        {
            this.topicClient = topicClientFactory.BuildTopicClient(topicName);
            this.sendRetryCount = sendRetryCount;
            this.sendRetryCooldownSeconds = sendRetryCooldownSeconds;
            this.logger = logger;
        }

        public async Task BatchPublish(IEnumerable<T> contentToPublish)
        {
            var localQueue = new Queue<ServiceBusMessage>();

            foreach (var content in contentToPublish)
            {
                var json = JsonConvert.SerializeObject(content);
                localQueue.Enqueue(new ServiceBusMessage(json));
            }


            while (localQueue.Count > 0)
            {
                var batch = await topicClient.CreateMessageBatchAsync();

                while (localQueue.Count > 0 && batch.TryAddMessage(localQueue.Peek()))
                {
                    localQueue.Dequeue();
                }

                await topicClient.SendBatchWithRetryAndWaitAsync(batch, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send batch message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }
    }
}