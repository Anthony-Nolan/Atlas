using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AppSettingsResponse
    {
        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }
    }
}