using System;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{

    internal class UpdateDatabaseResponse
    {
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
    }
}