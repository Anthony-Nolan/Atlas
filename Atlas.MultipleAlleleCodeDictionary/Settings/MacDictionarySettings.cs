namespace Atlas.MultipleAlleleCodeDictionary.Settings
{
    // TODO: ATLAS-431: Split into read/write settings
    public class MacDictionarySettings
    {
        public string AzureStorageConnectionString { get; set; }
        public string MacSourceUrl { get; set; }
        public string TableName { get; set; }
    }
}