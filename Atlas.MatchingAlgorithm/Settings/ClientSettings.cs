namespace Atlas.MatchingAlgorithm.ConfigSettings
{
    public abstract class ClientSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
    }

    public class DonorServiceSettings : ClientSettings
    {
        public string OverrideFilePath { get; set; }
    }

    public class HlaServiceSettings : ClientSettings
    {
    }
}