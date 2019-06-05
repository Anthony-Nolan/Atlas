using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public interface ICloudTableFactory
    {
        Task<CloudTable> GetTable(string tableReferenceString);
    }

    public class CloudTableFactory : ICloudTableFactory
    {
        public async Task <CloudTable> GetTable(string tableReferenceString)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(tableReferenceString);
            await tableReference.CreateIfNotExistsAsync();
            return new CloudTable(tableReference.StorageUri, tableClient.Credentials);
        }
    }
}