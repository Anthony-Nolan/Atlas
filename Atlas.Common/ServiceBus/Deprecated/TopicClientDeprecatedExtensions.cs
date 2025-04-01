using Microsoft.Azure.ServiceBus;
using Polly;
using System;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus.Deprecated
{
    public static class TopicClientDeprecatedExtensions
    {
        public static Task SendWithRetryAndWaitAsync(this Microsoft.Azure.ServiceBus.ITopicClient topicClient, Message message, int sendRetryCount, int sendRetryCooldownSeconds, Action<Exception, int> onRetryAction)
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
    }
}
