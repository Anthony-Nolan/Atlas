using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
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
        private readonly IAtlasLogger logger;
        private readonly Func<T, IReadOnlyDictionary<string, object>> applicationPropertiesSelector;

        /// <param name="applicationPropertiesSelector">
        /// Optional. Selects the values to stamp as Service Bus <see cref="ServiceBusMessage.ApplicationProperties"/> on each message,
        /// so they are visible without reading the body. See <see cref="ServiceBusMessageExtensions.AddApplicationProperties"/>.
        /// </param>
        public MessageBatchPublisher(
            ITopicClientFactory topicClientFactory,
            string topicName,
            int sendRetryCount,
            int sendRetryCooldownSeconds,
            IAtlasLogger logger,
            Func<T, IReadOnlyDictionary<string, object>> applicationPropertiesSelector = null)
        {
            this.topicClient = topicClientFactory.BuildTopicClient(topicName);
            this.sendRetryCount = sendRetryCount;
            this.sendRetryCooldownSeconds = sendRetryCooldownSeconds;
            this.logger = logger;
            this.applicationPropertiesSelector = applicationPropertiesSelector;
        }

        public async Task BatchPublish(IEnumerable<T> contentToPublish)
        {
            var localQueue = new Queue<ServiceBusMessage>();

            foreach (var content in contentToPublish)
            {
                var json = JsonConvert.SerializeObject(content);
                var message = new ServiceBusMessage(json);
                message.AddApplicationProperties(applicationPropertiesSelector?.Invoke(content));
                localQueue.Enqueue(message);
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