namespace Atlas.Debug.Client.Models.ServiceBus
{
    /// <summary>
    /// Request to peek at messages in a Service Bus subscription.
    /// </summary>
    public class PeekServiceBusMessagesRequest
    {
        /// <summary>
        /// Optional. The sequence number of the first message to peek. Defaults to 0.
        /// </summary>
        public long FromSequenceNumber { get; set; }

        /// <summary>
        /// The number of messages to peek.
        /// </summary>
        public int MessageCount { get; set; }
    }
}