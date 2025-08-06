using Azure.Messaging.ServiceBus;
using Polly;
using System;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus
{
    public static class TopicClientExtensions
    {
        public static Task SendWithRetryAndWaitAsync(this ITopicClient topicClient, ServiceBusMessage message, int sendRetryCount, int sendRetryCooldownSeconds, Action<Exception, int> onRetryAction)
        {
            if (sendRetryCount == 0)
            {
                throw new ArgumentException("sendRetryCount must be greater than 0");
            }

            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds),
                    onRetry: (exception, timespan, attemptNumber, context) => onRetryAction(exception, attemptNumber));

            return retryPolicy.ExecuteAsync(async () => await topicClient.SendAsync(message));
        }

        public static Task SendBatchWithRetryAndWaitAsync(this ITopicClient topicClient, ServiceBusMessageBatch messages, int sendRetryCount, int sendRetryCooldownSeconds, Action<Exception, int> onRetryAction)
        {
            if (sendRetryCount == 0)
            {
                throw new ArgumentException("sendRetryCount must be greater than 0");
            }

            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds),
                    onRetry: (exception, timespan, attemptNumber, context) => onRetryAction(exception, attemptNumber));

            return retryPolicy.ExecuteAsync(async () => await topicClient.SendBatchAsync(messages));
        }
    }
}