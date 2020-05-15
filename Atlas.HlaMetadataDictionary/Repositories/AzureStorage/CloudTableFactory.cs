using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    public interface ICloudTableFactory
    {
        Task<CloudTable> GetTable(string tableReferenceString);
    }

    public class CloudTableFactory : ICloudTableFactory
    {
        private readonly string storageConnectionString;
        public CloudTableFactory(string storageConnectionString)
        {
            this.storageConnectionString = storageConnectionString;
        }
        
        public async Task <CloudTable> GetTable(string tableReferenceString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(tableReferenceString);
            await tableReference.CreateIfNotExistsAsync();
            return new CloudTable(tableReference.StorageUri, tableClient.Credentials);
        }
    }
}