namespace Atlas.MultipleAlleleCodeDictionary.Settings
{
    /// <summary>
    /// Settings needed in all use-cases of the MAC Dictionary component.
    /// </summary>
    public class MacDictionarySettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string TableName { get; set; }
    }
}