// ReSharper disable InconsistentNaming
namespace Nova.SearchAlgorithm.Clients.AzureManagement.AzureApiModels.Database
{
    internal class DatabaseOperationResponse
    {
        public DatabaseOperationProperties properties { get; set; }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class DatabaseOperationProperties
    {
        public int percentComplete { get; set; }
        public string operation { get; set; }
        public string state { get; set; }
        public string startTime { get; set; }
    }
}