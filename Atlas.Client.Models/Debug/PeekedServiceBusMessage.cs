namespace Atlas.Client.Models.Debug
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