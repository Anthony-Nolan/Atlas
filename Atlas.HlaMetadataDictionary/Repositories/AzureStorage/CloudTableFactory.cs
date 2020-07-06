using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    internal interface ICloudTableFactory
    {
        Task<CloudTable> GetTable(string tableReferenceString);
    }

    internal class CloudTableFactory : ICloudTableFactory
    {
        private readonly string storageConnectionString;

        public CloudTableFactory(HlaMetadataDictionarySettings settings)
        {
            storageConnectionString = settings.AzureStorageConnectionString;
        }

        public async Task<CloudTable> GetTable(string tableReferenceString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(tableReferenceString);
            await tableReference.CreateIfNotExistsAsync();
            return tableReference;
        }
    }
}