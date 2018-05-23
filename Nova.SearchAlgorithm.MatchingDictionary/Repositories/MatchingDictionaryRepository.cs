using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

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
            InsertMatchingDictionaryEntriesIntoTable(dictionaryContents.ToList());
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

        private void InsertMatchingDictionaryEntriesIntoTable(IReadOnlyCollection<MatchingDictionaryEntry> contents)
        {
            foreach (var partition in LocusNames.MatchLoci)
            {
                var entitiesForPartition = contents
                    .Where(entry => entry.MatchLocus.Equals(partition))
                    .Select(entry => entry.ToTableEntity())
                    .ToList();

                for (var i = 0; i < entitiesForPartition.Count; i = i + BatchSize)
                {
                    var batchToInsert = entitiesForPartition.Skip(i).Take(BatchSize).ToList();
                    BatchInsertIntoTable(batchToInsert);
                }
            }
        }

        private void BatchInsertIntoTable(List<MatchingDictionaryTableEntity> entities)
        {
            var batchOperation = new TableBatchOperation();
            entities.ForEach(entity => batchOperation.Insert(entity));
            table.ExecuteBatch(batchOperation);
        }
    }
}
