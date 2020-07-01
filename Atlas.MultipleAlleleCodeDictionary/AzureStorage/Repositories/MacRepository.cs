using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Cosmos.Table;
using QueryComparisons = Microsoft.WindowsAzure.Storage.Table.QueryComparisons;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories
{
    internal interface IMacRepository
    {
        public Task<string> GetLastMacEntry();
        public Task InsertMacs(IEnumerable<Mac> macCodes);
        public Task<Mac> GetMac(string macCode);
        public Task<IEnumerable<Mac>> GetAllMacs();
    }

    internal class MacRepository : IMacRepository
    {
        protected readonly CloudTable Table;

        public MacRepository(MacDictionarySettings macDictionarySettings)
        {
            var connectionString = macDictionarySettings.AzureStorageConnectionString;
            var tableName = macDictionarySettings.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            Table = tableClient.GetTableReference(tableName);
        }

        public async Task<string> GetLastMacEntry()
        {
            var query = new TableQuery<MacEntity>();
            var result = await Table.ExecuteQueryAsync(query);
            // MACs are alphabetical within a character length - any new MACs are appended to the end of the list alphabetically.
            // Order by partition key to ensure that longer MACs are returned later.
            // e.g. purely alphabetically, ABC comes before ZX, but as it has fewer character, ZX is actually the earlier MAC
            return result
                .OrderByDescending(x => x.MacLength)
                .ThenByDescending(x => x.Mac).FirstOrDefault()?.Mac;
        }

        public async Task InsertMacs(IEnumerable<Mac> macs)
        {
            await Table.BatchInsert(macs.Select(mac => new MacEntity(mac)));
        }

        public async Task<Mac> GetMac(string macCode)
        {
            var query = new TableQuery<MacEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, macCode.Length.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, macCode)
                    ));
            var result = await Table.ExecuteQueryAsync(query);
            return new Mac(result.Single());
        }

        public async Task<IEnumerable<Mac>> GetAllMacs()
        {
            var query = new TableQuery<MacEntity>();
            var result = await Table.ExecuteQueryAsync(query);
            return result.Select(x => new Mac(x));
        }
    }
}