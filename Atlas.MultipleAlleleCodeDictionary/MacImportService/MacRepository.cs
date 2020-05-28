using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Models;
using Microsoft.Azure.Cosmos.Table;
using MoreLinq;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService
{
    public interface IMacRepository
    {
        public string GetLastMacEntry();
        public Task InsertMacs(List<MultipleAlleleCodeEntity> macCodes);
    }

    public class MacRepository : IMacRepository
    {
        private readonly CloudTable table;

        public MacRepository(IOptions<MacImportSettings> macImportSettings)
        {
            var connectionString = macImportSettings.Value.ConnectionString;
            var tableName = macImportSettings.Value.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            table = tableClient.GetTableReference(tableName);
        }

        public string GetLastMacEntry()
        {
            var query = new TableQuery<MultipleAlleleCodeEntity>();
            var result = table.ExecuteQuery(query);
            // MACs are alphabetical - any new MACs are appended to the end of the list alphabetically.
            return result.OrderByDescending(x => x.PartitionKey).First().PartitionKey;
        }

        public async Task InsertMacs(List<MultipleAlleleCodeEntity> macs)
        {
            foreach (var macBatch in macs.Batch(100))
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