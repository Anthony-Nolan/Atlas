using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        void RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
    }

    public class MatchingDictionaryRepository : IMatchingDictionaryRepository
    {
        private const int BatchSize = 100;
        private const string TableReference = "MatchingDictionary";
        private readonly ICloudTableFactory tableFactory;
        private CloudTable table;

        public MatchingDictionaryRepository(ICloudTableFactory factory)
        {
            tableFactory = factory;
            GetTable();
        }

        public void RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            DropCreateTable();
            InsertMatchingDictionaryEntriesIntoTable(dictionaryContents);
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var retrieveOperation = TableOperation.Retrieve<MatchingDictionaryTableEntity>(partition, rowKey);            
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            var entry = ((MatchingDictionaryTableEntity) tableResult.Result)?.ToMatchingDictionaryEntry();

            return entry;
        }

        private void GetTable()
        {
            table = tableFactory.GetTable(TableReference);
        }

        private void DropCreateTable()
        {
            table.Delete();
            GetTable();
        }

        private void InsertMatchingDictionaryEntriesIntoTable(IEnumerable<MatchingDictionaryEntry> contents)
        {
            var contentsList = contents.ToList();

            foreach (var partition in PermittedLocusNames.GetPermittedMatchLoci())
            {
                var entitiesForPartitionList = contentsList
                    .Where(entry => entry.MatchLocus.Equals(partition))
                    .Select(entry => entry.ToTableEntity())
                    .ToList();

                for (var i = 0; i < entitiesForPartitionList.Count; i = i + BatchSize)
                {
                    var batchToInsert = entitiesForPartitionList.Skip(i).Take(BatchSize);
                    BatchInsertIntoTable(batchToInsert);
                }
            }
        }

        private void BatchInsertIntoTable(IEnumerable<MatchingDictionaryTableEntity> entities)
        {
            var batchOperation = new TableBatchOperation();
            entities.ToList().ForEach(entity => batchOperation.Insert(entity));
            table.ExecuteBatch(batchOperation);
        }
    }
}
