using Nova.SearchAlgorithm.Config;
using Nova.Utils.WebApi.Client;

namespace Nova.SearchAlgorithm.Helpers
{
    public class ClientSettingsProvider
    {
        public ClientSettings GetHlaServiceClientSettings()
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