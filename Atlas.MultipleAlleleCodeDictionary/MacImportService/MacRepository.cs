using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage.Extensions;
using Atlas.MultipleAlleleCodeDictionary.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings.MacImport;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using MoreLinq;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService
{
    public interface IMacRepository
    {
        public Task<string> GetLastMacEntry();
        public Task InsertMacs(IEnumerable<MultipleAlleleCodeEntity> macCodes);
    }

    public class MacRepository : IMacRepository
    {
        /// <summary>
        /// The maximum BatchSize for inserting to an Azure Storage Table is 100. This cannot be > 100 for this reason.
        /// </summary>
        private const int BatchSize = 100;

        private readonly CloudTable table;

        public MacRepository(IOptions<MacImportSettings> macImportSettings)
        {
            var connectionString = macImportSettings.Value.ConnectionString;
            var tableName = macImportSettings.Value.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            table = tableClient.GetTableReference(tableName);
        }

        public async Task<string> GetLastMacEntry()
        {
            var query = new TableQuery<MultipleAlleleCodeEntity>();
            var result = await table.ExecuteQueryAsync(query);
            // MACs are alphabetical - any new MACs are appended to the end of the list alphabetically.
            return result.OrderByDescending(x => x.RowKey).FirstOrDefault()?.RowKey ?? "";
        }

        public async Task InsertMacs(IEnumerable<MultipleAlleleCodeEntity> macs)
        {
            foreach (var macBatch in macs.Batch(BatchSize))
            {
                var batchOp = new TableBatchOperation();
                foreach (var mac in macBatch)
                {
                    batchOp.Insert(mac);
                }

                await table.ExecuteBatchAsync(batchOp);
            }
        }
    }
}