using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    /// <summary>
    /// Useful, generic extensions that capture common cosmos operations.
    /// </summary>
    public static class CosmosClientExtensions
    {
        private static string DatabaseId = ConfigurationManager.AppSettings["cosmos.database"];

        public static string CollectionId<T>(this DocumentClient client)
        {
            return typeof(T).Name;
        }

        public static async Task<T> GetItemAsync<T>(this DocumentClient client, string id) where T : class
        {
            try
            {
                Document document =
                    await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, client.CollectionId<T>(), id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync<T>(this DocumentClient client, Expression<Func<T, bool>> predicate) where T : class
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, client.CollectionId<T>()),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync<T>(this DocumentClient client, T item) where T : class
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, client.CollectionId<T>()), item);
        }

        public static async Task<Document> UpdateItemAsync<T>(this DocumentClient client, string id, T item) where T : class
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, client.CollectionId<T>(), id), item);
        }

        public static async Task DeleteItemAsync<T>(this DocumentClient client, string id) where T : class
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, client.CollectionId<T>(), id));
        }

        public static async Task CreateDatabaseIfNotExistsAsync(this DocumentClient client)
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task CreateCollectionIfNotExistsAsync<T>(this DocumentClient client) where T : class
        {
            var CollectionId = client.CollectionId<T>();
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection
                        {
                            Id = CollectionId
                        },
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
