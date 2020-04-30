using System;

namespace Atlas.Utils.ServiceBus.Models
{
    public class ServiceBusMessage<T>
    {
        public long SequenceNumber { get; set; }
        public string LockToken { get; set; }
        public DateTime LockedUntilUtc { get; set; }
        public T DeserializedBody { get; set; }
    }
}
