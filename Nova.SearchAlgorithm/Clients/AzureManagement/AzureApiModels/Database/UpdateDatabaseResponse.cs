using System;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class UpdateDatabaseResponse
    {
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
    }
}