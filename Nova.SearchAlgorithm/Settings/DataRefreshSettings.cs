using Nova.SearchAlgorithm.Models.AzureManagement;

namespace Nova.SearchAlgorithm.Settings
{
    public class DataRefreshSettings
    {
        public AzureDatabaseSize ActiveDatabaseSize { get; set; }
        public AzureDatabaseSize DormantDatabaseSize { get; set; }
        public AzureDatabaseSize RefreshDatabaseSize { get; set; }
        
        public string DonorFunctionsAppName { get; set; }
        public string DonorImportFunctionName { get; set; }
        
        public string DatabaseAName { get; set; }
        public string DatabaseBName { get; set; }
    }
}