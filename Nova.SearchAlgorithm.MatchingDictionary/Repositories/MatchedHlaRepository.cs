using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchedHlaRepository
    {
        void RecreateDictionaryTable(IEnumerable<IMatchedHla> dictionaryContents);
    }

    public class MatchedHlaRepository : IMatchedHlaRepository
    {
        private const int BatchSize = 100;
        private const string TableReference = "MatchedHlaDictionary";
        private readonly CloudTable selectedDictionaryTable;

        public MatchedHlaRepository()
        {
            selectedDictionaryTable = new CloudTableFactory().GetTable(TableReference);
        }

        public void RecreateDictionaryTable(IEnumerable<IMatchedHla> dictionaryContents)
        {
            DeleteDictionaryTable();

            var contents = dictionaryContents.ToList();
            foreach (var partition in LocusNames.MatchLoci)
            {
                BatchInsertIntoDictionaryTable(
                    contents
                        .Where(hla => hla.HlaType.MatchLocus.Equals(partition))
                        .Select(hla => hla.ToTableEntity())
                        .ToList()
                        );
            }
        }

        private void DeleteDictionaryTable()
        {
            selectedDictionaryTable.DeleteIfExists();
        }

        private void BatchInsertIntoDictionaryTable(IReadOnlyCollection<MatchedHlaTableEntity> entities)
        {
            var batchOperation = new TableBatchOperation();

            for (var i = 0; i < entities.Count; i = i + BatchSize)
            {
                var batchToInsert = entities.Skip(i).Take(BatchSize).ToList();
                batchToInsert.ForEach(entity => batchOperation.Insert(entity));
            }

            selectedDictionaryTable.ExecuteBatch(batchOperation);
        }
    }
}
