using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public interface ICloudTableFactory
    {
        CloudTable GetOrCreateTable(string tableReferenceString);
    }

    public class CloudTableFactory : ICloudTableFactory
    {
        public CloudTable GetOrCreateTable(string tableReferenceString)
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableReference = tableClient.GetTableReference(tableReferenceString);
            tableReference.CreateIfNotExists();
            return new CloudTable(tableReference.StorageUri, tableClient.Credentials);
        }
    }
}