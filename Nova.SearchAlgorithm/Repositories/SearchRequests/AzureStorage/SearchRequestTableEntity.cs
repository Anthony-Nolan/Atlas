using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage
{
    public class SearchRequestTableEntity : TableEntity
    {
        //todo: define properties
        public string SerialisedSearchRequest { get; set; }
        
        public SearchRequestTableEntity() { }

        public SearchRequestTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
    }
}