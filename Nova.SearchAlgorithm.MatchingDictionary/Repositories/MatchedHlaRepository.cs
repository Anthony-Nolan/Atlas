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
        IMatchedHla GetMatchedHlaWhereExactMatch(string partition, string rowKey);
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

        public void RecreateDictionaryTable(IEnumerable<IMatchedHla> dictionaryContents)
        {
            DropCreateTable();
            InsertContentsIntoDictionaryTable(dictionaryContents.ToList());
        }

        public IMatchedHla GetMatchedHlaWhereExactMatch(string partition, string rowKey)
        {
            return table
                .CreateQuery<MatchedHlaTableEntity>()
                .Where(entity => entity.PartitionKey.Equals(partition) && entity.RowKey.Equals(rowKey))
                .FirstOrDefault()
                ?.ToMatchedHla();
        }

        private void GetDictionaryTable()
        {
            table = tableFactory.GetTable(TableReference);
        }

        private void DropCreateTable()
        {
            table.DeleteIfExists();
            GetDictionaryTable();
        }

        private void InsertContentsIntoDictionaryTable(IReadOnlyCollection<IMatchedHla> contents)
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
                    BatchInsertIntoDictionaryTable(batchToInsert);
                }
            }
        }

        private void BatchInsertIntoDictionaryTable(List<MatchedHlaTableEntity> entities)
        {
            var batchOperation = new TableBatchOperation();
            entities.ForEach(entity => batchOperation.Insert(entity));
            table.ExecuteBatch(batchOperation);
        }
    }
}
