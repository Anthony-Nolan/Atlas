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
        private readonly ICloudTableFactory tableFactory;

        public MatchedHlaRepository(ICloudTableFactory factory)
        {
            tableFactory = factory;
        }

        public void RecreateDictionaryTable(IEnumerable<IMatchedHla> dictionaryContents)
        {
            var table = DropCreateTable();
            InsertContentsIntoDictionaryTable(dictionaryContents.ToList(), table);
        }

        private CloudTable DropCreateTable()
        {
            var table = tableFactory.GetTable(TableReference);
            table.DeleteIfExists();
            return tableFactory.GetTable(TableReference);
        }

        private static void InsertContentsIntoDictionaryTable(IReadOnlyCollection<IMatchedHla> contents, CloudTable table)
        {
            foreach (var partition in LocusNames.MatchLoci)
            {
                var entitiesForPartition = contents
                    .Where(hla => hla.HlaType.MatchLocus.Equals(partition))
                    .Select(hla => hla.ToTableEntity())
                    .ToList();

                for (var i = 0; i < entitiesForPartition.Count; i = i + BatchSize)
                {
                    var batchToInsert = entitiesForPartition.Skip(i).Take(BatchSize).ToList();
                    BatchInsertIntoDictionaryTable(batchToInsert, table);
                }
            }
        }
        
        private static void BatchInsertIntoDictionaryTable(List<MatchedHlaTableEntity> entities, CloudTable table)
        {
            var batchOperation = new TableBatchOperation();
            entities.ForEach(entity => batchOperation.Insert(entity));
            table.ExecuteBatch(batchOperation);
        }
    }
}
