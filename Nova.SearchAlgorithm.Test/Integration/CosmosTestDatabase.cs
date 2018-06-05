using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Repositories.Donors.AzureStorage;
using Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class CosmosTestDatabase
    {
        private readonly string DatabaseId = ConfigurationManager.AppSettings["cosmos.database"];
        private readonly DocumentClient client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["cosmos.endpoint"]), ConfigurationManager.AppSettings["cosmos.authKey"]);

        public void Clear()
        {
            Task.WhenAll(
                DeleteCollectionIfExists(typeof(DonorCosmosDocument).FullName),
                DeleteCollectionIfExists(typeof(PotentialHlaMatchRelationCosmosDocument).FullName)
                ).Wait();
        }

        private async Task DeleteCollectionIfExists(string collectionId)
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId));
                await client.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId));
            }
            catch (DocumentClientException)
            {
                // NOP: either the DB or the collection does not exist
            }
        }
    }

    
}
