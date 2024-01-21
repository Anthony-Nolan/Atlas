namespace Atlas.Debug.Client.Models.ServiceBus
{
    public class PeekedServiceBusMessage<T>
    {
        public T DeserializedBody { get; }

        public PeekedServiceBusMessage(T deserializedBody)
        {
            DeserializedBody = deserializedBody;
        }
    }
}