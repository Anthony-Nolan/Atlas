using System;
using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{

    internal class DatabaseOperationResponse
    {
        [JsonProperty("value")]
        public IEnumerable<DatabaseOperationValue> Value { get; set; }
    }

    internal class DatabaseOperationValue
    {
        [JsonProperty("properties")]
        public DatabaseOperationProperties Properties { get; set; }
    }

    internal class DatabaseOperationProperties
    {
        [JsonProperty("percentComplete")]
        public int PercentComplete { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("state")]
        public AzureDatabaseOperationState State { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("databaseName")]
        public string DatabaseName { get; set; }
    }
}