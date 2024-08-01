using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.ServiceBus
{
    /// <summary>
    /// Represents the response from a <see cref="PeekServiceBusMessagesRequest"/>.
    /// </summary>
    public class PeekServiceBusMessagesResponse<T>
    {
        /// <summary>
        /// The number of messages that were retrieved from the target service bus subscription.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Messages that were peeked from the target service bus subscription.
        /// </summary>
        public IEnumerable<T> PeekedMessages { get; set; }

        /// <summary>
        /// The sequence number of the last message returned.
        /// </summary>
        public long? LastSequenceNumber { get; set; }
    }
}