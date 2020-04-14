using System;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class UpdateDatabaseResponse
    {
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
    }
}