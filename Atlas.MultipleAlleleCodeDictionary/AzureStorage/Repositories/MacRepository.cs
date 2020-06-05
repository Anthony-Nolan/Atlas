using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Amqp.Serialization;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using MoreLinq;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories
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

        protected readonly CloudTable table;

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
            return result
                .OrderByDescending(x => int.Parse(x.PartitionKey))
                .ThenByDescending(x => x.RowKey).FirstOrDefault()?.RowKey;
        }

        public async Task InsertMacs(IEnumerable<MultipleAlleleCodeEntity> macs)
        {
            var macsByLength = macs.GroupBy(x => x.PartitionKey);
            foreach (var macsOfSameLength in macsByLength)
            {
                foreach (var macBatch in macsOfSameLength.Batch(BatchSize))
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
}