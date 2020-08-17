namespace Atlas.MultipleAlleleCodeDictionary.Settings
{
    /// <summary>
    /// Settings needed for download of MACs from source.
    /// Note: <see cref="MacDictionarySettings"/> contains configuration settings for the MAC dictionary itself.
    /// </summary>
    public class MacDownloadSettings
    {
        public string MacSourceUrl { get; set; }
    }
}