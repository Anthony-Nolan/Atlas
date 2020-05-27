using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings.MacImport;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService{
    public interface IMacRepository
    {
        public string GetLastMacEntry();
        public Task InsertMacs(List<MultipleAlleleCodeEntity> macCodes);
    }
    
    public class MacRepository : IMacRepository
    {

        private CloudTable Table { get; set; }
        
        public MacRepository(IOptions<MacImportSettings> messagingServiceBusSettings)
        {
            var connectionString = messagingServiceBusSettings.Value.ConnectionString;
            var tableName = messagingServiceBusSettings.Value.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            Table = tableClient.GetTableReference(tableName);
        }
        
        public string GetLastMacEntry()
        {
            var query = new TableQuery<MultipleAlleleCodeEntity>();
            return Table.ExecuteQuery(query).OrderByDescending(x => x.PartitionKey).First().PartitionKey;
        }

        public async Task InsertMacs(List<MultipleAlleleCodeEntity> objects){
            // ExecuteBatchAsync can only perform 100 operations at a maximum
            for (var i = 0; i < objects.Count - 100 ; i += 100)
            {
                var batchOp = new TableBatchOperation();
                for (var j = 0; j < 100; j++)
                {
                    batchOp.Insert(objects[i + j]);
                }

                await Table.ExecuteBatchAsync(batchOp);
                
                
            }
        }
    }
    
    
}