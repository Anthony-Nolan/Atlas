namespace Atlas.DonorImport.Settings.ServiceBus
{
    internal class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string MatchingDonorUpdateTopic { get; set; }
    }
}