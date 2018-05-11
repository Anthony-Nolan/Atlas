using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchedHlaRepository
    {
        void RecreateDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetDictionaryEntry(string matchLocus, string lookupName, TypingMethod typingMethod);
    }

    public class MatchedHlaRepository : IMatchedHlaRepository
    {
        private const int BatchSize = 100;
        private const string TableReference = "MatchedHlaDictionary";
        private readonly ICloudTableFactory tableFactory;
        private CloudTable table;

        public MatchedHlaRepository(ICloudTableFactory factory)
        {
            tableFactory = factory;
            GetDictionaryTable();
        }

        public void RecreateDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            DropCreateTable();
            InsertContentsIntoDictionaryTable(dictionaryContents.ToList());
        }

        public async Task<MatchingDictionaryEntry> GetDictionaryEntry(string matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var rowKey = DictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var retrieveOperation = TableOperation.Retrieve<DictionaryTableEntity>(matchLocus, rowKey);            
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
