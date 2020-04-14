using System;

namespace Atlas.MatchingAlgorithm.Models.AzureManagement
{
    public class DatabaseOperation
    {
        public string DatabaseName { get; set; }
        public string Operation { get; set; }
        public int PercentComplete { get; set; }
        public AzureDatabaseOperationState State { get; set; }
        public DateTime StartTime { get; set; }
    }
}