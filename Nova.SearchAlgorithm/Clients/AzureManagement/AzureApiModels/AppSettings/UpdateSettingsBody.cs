using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.AppSettings
{
    internal class UpdateSettingsBody
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }
        
        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }
    }
}