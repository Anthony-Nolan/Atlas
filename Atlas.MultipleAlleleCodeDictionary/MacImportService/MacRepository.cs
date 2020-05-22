using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Models;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportService{
    public interface IMacRepository
    {
        public string GetLastMacEntry();
        public Task InsertMacs(List<MultipleAlleleCodeEntity> macCodes);
    }
    
    public class MacRepository : IMacRepository
    {

        private CloudTable Table { get; set; }
        
        public MacRepository()
        {
            var connectionString  = AppSettings.LoadAppSettings().StorageConnectionString;
            connectionString =
                "DefaultEndpointsProtocol=https;AccountName=devatlasstorage;AccountKey=ELvrCzBAaZBrpmSgZhV1X18q619mv3+dp+ldsd6D6QGYAEIVTO1c690eOiK4HmyBBmrczjW4gK6BZjE54ZJggA==;EndpointSuffix=core.windows.net";
            
            var tableName = AppSettings.LoadAppSettings().TableName;
            tableName = "AtlasMultipleAlleleCodes";
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