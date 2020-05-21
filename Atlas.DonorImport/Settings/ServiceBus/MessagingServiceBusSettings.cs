namespace Atlas.DonorImport.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string MatchingDonorUpdateTopic { get; set; }
    }
}