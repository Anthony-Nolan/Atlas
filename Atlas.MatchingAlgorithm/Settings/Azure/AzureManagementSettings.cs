namespace Atlas.MatchingAlgorithm.Settings.Azure
{
    public class AzureAuthenticationSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
    
    public class AzureManagementSettings
    {
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
    }

    public class AzureAppServiceManagementSettings : AzureManagementSettings
    {
    }

    public class AzureDatabaseManagementSettings : AzureManagementSettings
    {
        public string ServerName { get; set; }
        public string PollingRetryIntervalMilliseconds { get; set; }
    }
}