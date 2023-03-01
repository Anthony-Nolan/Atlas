namespace Atlas.DonorImport.ExternalInterface.Settings
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string DonorFileBlobContainer { get; set; }
        public string DonorIdCheckerResultsBlobContainer { get; set; }
    }
}
