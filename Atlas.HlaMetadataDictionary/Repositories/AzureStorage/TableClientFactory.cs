using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Azure.Data.Tables;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    internal interface ITableClientFactory
    {
        Task<TableClient> GetTable(string tableReferenceString);
    }

    internal class TableClientFactory : ITableClientFactory
    {
        private readonly TableServiceClient serviceClient; 

        public TableClientFactory(HlaMetadataDictionarySettings settings)
        {
            this.serviceClient = new TableServiceClient(settings.AzureStorageConnectionString);
        }

        public async Task<TableClient> GetTable(string tableReferenceString)
        {
            var tableClient = serviceClient.GetTableClient(tableReferenceString);
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }
    }
}