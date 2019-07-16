using Nova.SearchAlgorithm.Models.AzureManagement;

namespace Nova.SearchAlgorithm.Settings
{
    public class DataRefreshSettings
    {
        public string ActiveDatabaseSize { get; set; }
        public string DormantDatabaseSize { get; set; }
        public string RefreshDatabaseSize { get; set; }
        
        public string DonorFunctionsAppName { get; set; }
        public string DonorImportFunctionName { get; set; }
        
        public string DatabaseAName { get; set; }
        public string DatabaseBName { get; set; }
    }
}