namespace Atlas.DonorImport.ExternalInterface.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string UpdatedSearchableDonorsTopic { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
        public string ImportFileSubscription { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
        public string ImportFileTopic { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
        public string DonorIdCheckerTopic { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
        public string DonorIdCheckerSubscription { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
        public string DonorIdCheckerResultsTopic { get; set; }

        // ReSharper disable once UnusedMember.Global - Used in function binding, and included here for completeness
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