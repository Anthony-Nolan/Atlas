namespace Nova.SearchAlgorithm.Settings
{
    public abstract class ClientSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
    }

    public class DonorServiceSettings : ClientSettings
    {
    }

    public class HlaServiceSettings : ClientSettings
    {
    }
}