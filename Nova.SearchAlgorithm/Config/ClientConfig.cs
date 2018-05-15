using Nova.SearchAlgorithm.Config;
using Nova.Utils.WebApi.Client;

namespace Nova.SearchAlgorithm.Config
{
    /// <summary>
    /// Provides configuration for those Nova microservice client libraries used by this service.
    /// </summary>
    public class ClientConfig
    {
        public static ClientSettings GetHlaServiceClientSettings()
        {
            return new ClientSettings
            {
                ApiKey = Configuration.HlaServiceApiKey,
                BaseUrl = Configuration.HlaServiceBaseUrl,
                ClientName = "hla_service_client",
                JsonSettings = JsonConfig.GlobalSettings
            };
        }
    }
}