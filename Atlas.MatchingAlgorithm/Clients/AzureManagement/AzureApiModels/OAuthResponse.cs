using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels
{

    internal class OAuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}