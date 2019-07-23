using Newtonsoft.Json;

namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class OAuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}