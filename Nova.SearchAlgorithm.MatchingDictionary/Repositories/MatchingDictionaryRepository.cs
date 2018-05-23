using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
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
            GetDictionaryTable();
        }

        public void RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            DropCreateTable();
            InsertContentsIntoDictionaryTable(dictionaryContents.ToList());
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = DictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = DictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var retrieveOperation = TableOperation.Retrieve<DictionaryTableEntity>(partition, rowKey);            
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            var entry = ((DictionaryTableEntity) tableResult.Result)?.ToDictionaryEntry();

            return entry;
        }

        private void GetDictionaryTable()
        {
            table = tableFactory.GetTable(TableReference);
        }

        private void DropCreateTable()
        {
            table.Delete();
            GetDictionaryTable();
        }

        private void InsertContentsIntoDictionaryTable(IReadOnlyCollection<MatchingDictionaryEntry> contents)
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
                    BatchInsertIntoDictionaryTable(batchToInsert);
                }
            }
        }

        private void BatchInsertIntoDictionaryTable(List<DictionaryTableEntity> entities)
        {
            var batchOperation = new TableBatchOperation();
            entities.ForEach(entity => batchOperation.Insert(entity));
            table.ExecuteBatch(batchOperation);
        }
    }
}
