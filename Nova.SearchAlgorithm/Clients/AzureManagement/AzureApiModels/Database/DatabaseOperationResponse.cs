// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Models.AzureManagement;

namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DatabaseOperationResponse
    {
        public IEnumerable<DatabaseOperationValue> value { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DatabaseOperationValue
    {
        public DatabaseOperationProperties properties { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DatabaseOperationProperties
    {
        public int percentComplete { get; set; }
        public string operation { get; set; }
        public AzureDatabaseOperationState state { get; set; }
        public DateTime startTime { get; set; }
        public string databaseName { get; set; }
    }
}