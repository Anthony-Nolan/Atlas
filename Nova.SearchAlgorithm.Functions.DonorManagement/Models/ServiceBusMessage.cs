namespace Nova.SearchAlgorithm.Functions.DonorManagement.Models
{
    public class ServiceBusMessage<T>
    {
        public long SequenceNumber { get; set; }
        public T DeserializedBody { get; set; }
    }
}
