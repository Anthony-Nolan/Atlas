namespace Atlas.DonorImport.ExternalInterface.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string UpdatedSearchableDonorsTopic { get; set; }

        public string ImportFileSubscription { get; set; }

        public string ImportFileTopic { get; set; }

        public string DonorIdCheckerTopic { get; set; }

        public string DonorIdCheckerSubscription { get; set; }

        public string DonorIdCheckerResultsTopic { get; set; }

        public string DonorInfoCheckerResultsTopic { get; set; }
        
        public string DonorImportResultsTopic { get; set; }

        public int SendRetryCount { get; set; }

        public int SendRetryCooldownSeconds { get; set; }

        /// <summary>
        /// Required by debug endpoint that peeks donor import results messages.
        /// </summary>
        public string DonorImportResultsDebugSubscription { get; set; }
    }
}