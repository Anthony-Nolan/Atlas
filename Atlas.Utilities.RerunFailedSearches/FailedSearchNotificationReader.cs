using Atlas.Client.Models.Search.Results;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Utilities.RerunFailedSearches
{
    /// <summary>A failed search found on a results-notification topic.</summary>
    public record FailedNotification(string SearchRequestId, string? RepeatSearchRequestId, DateTimeOffset EnqueuedTimeUtc);

    public interface IFailedSearchNotificationReader
    {
        /// <summary>
        /// Peeks the given topic/subscription and returns every <see cref="SearchResultsNotification"/> that
        /// was unsuccessful and enqueued at or after <paramref name="fromUtc"/>. Peeking does not consume the
        /// messages, so this is a safe read-only operation.
        /// </summary>
        Task<IReadOnlyCollection<FailedNotification>> GetFailedSince(string topic, string subscription, DateTimeOffset fromUtc);
    }

    public class FailedSearchNotificationReader : IFailedSearchNotificationReader
    {
        private const int PeekBatchSize = 100;
        private readonly ServiceBusClient client;

        public FailedSearchNotificationReader(ServiceBusClient client)
        {
            this.client = client;
        }

        public async Task<IReadOnlyCollection<FailedNotification>> GetFailedSince(string topic, string subscription, DateTimeOffset fromUtc)
        {
            await using var receiver = client.CreateReceiver(topic, subscription);

            var failed = new List<FailedNotification>();
            long fromSequenceNumber = 0;

            // Peek pages ascending by sequence number until a page comes back empty. Enqueue time is only
            // available on the raw received message, so we read it here rather than via the shared peeker.
            while (true)
            {
                var batch = await receiver.PeekMessagesAsync(PeekBatchSize, fromSequenceNumber);
                if (batch is null || batch.Count == 0)
                {
                    break;
                }

                foreach (var message in batch)
                {
                    // Spec: only messages enqueued strictly after the input date.
                    if (message.EnqueuedTime <= fromUtc)
                    {
                        continue;
                    }

                    var notification = JsonConvert.DeserializeObject<SearchResultsNotification>(message.Body.ToString());
                    if (notification is { WasSuccessful: false })
                    {
                        failed.Add(new FailedNotification(
                            notification.SearchRequestId,
                            notification.RepeatSearchRequestId,
                            message.EnqueuedTime));
                    }
                }

                fromSequenceNumber = batch.Max(m => m.SequenceNumber) + 1;
            }

            return failed;
        }
    }
}
