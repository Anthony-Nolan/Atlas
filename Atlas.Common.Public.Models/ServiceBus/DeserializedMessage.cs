namespace Atlas.Common.Public.Models.ServiceBus
{
    /// <summary>
    /// Service bus message with deserialized body of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeserializedMessage<T>
    {
        public long SequenceNumber { get; set; }
        public object LockToken { get; set; }
        public DateTime LockedUntilUtc { get; set; }
        public T DeserializedBody { get; set; }
    }
}
