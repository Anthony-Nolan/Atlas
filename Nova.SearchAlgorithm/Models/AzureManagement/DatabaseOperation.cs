using System;

namespace Nova.SearchAlgorithm.Models.AzureManagement
{
    public class DatabaseOperation
    {
        public string DatabaseName { get; set; }
        public string Operation { get; set; }
        public int PercentComplete { get; set; }
        public string State { get; set; }
        public DateTime StartTime { get; set; }
    }
}