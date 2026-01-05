using System;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Azure.Core;
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
            var options = new TableClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = settings.MaxRetries,
                    MaxDelay = TimeSpan.FromSeconds(settings.MaxDelay)
                }
            };
            serviceClient = new TableServiceClient(settings.AzureStorageConnectionString, options);
        }

        public async Task<TableClient> GetTable(string tableReferenceString)
        {
            var tableClient = serviceClient.GetTableClient(tableReferenceString);
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }
    }
}