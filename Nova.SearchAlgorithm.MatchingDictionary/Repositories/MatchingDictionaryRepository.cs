using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
    }

    public class MatchingDictionaryRepository : IMatchingDictionaryRepository
    {
        private const int BatchSize = 100;
        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;

        public MatchingDictionaryRepository(ICloudTableFactory factory, ITableReferenceRepository tableReferenceRepository)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
        }

        public async Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            var newDataTable = CreateNewDataTable();
            InsertMatchingDictionaryEntriesIntoDataTable(dictionaryContents, newDataTable);
            await tableReferenceRepository.InsertOrUpdateMatchingDictionaryTableReference(newDataTable.Name);
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {            
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var dataTable = await GetOrCreateDataTable();
            var result = await CloudTableQueries.RetrieveResultFromTableByPartitionAndRowKey<MatchingDictionaryTableEntity>(partition, rowKey, dataTable);

            return result?.ToMatchingDictionaryEntry();
        }

        private CloudTable CreateNewDataTable()
        {
            var dataTableReference = tableReferenceRepository.CreateNewMatchingDictionaryTableReference();
            return tableFactory.GetOrCreateTable(dataTableReference);
        }

        private async Task<CloudTable> GetOrCreateDataTable()
        {
            var dataTableReference =
                await tableReferenceRepository.GetMatchingDictionaryTableReferenceIfExistsElseEmptyString();

            return dataTableReference.Equals(string.Empty)
                ? CreateNewDataTable()
                : tableFactory.GetOrCreateTable(dataTableReference);
        }

        private static void InsertMatchingDictionaryEntriesIntoDataTable(IEnumerable<MatchingDictionaryEntry> contents, CloudTable dataTable)
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
                    BatchInsertIntoTable(batchToInsert, dataTable);
                }
            }
        }

        private static void BatchInsertIntoTable(IEnumerable<MatchingDictionaryTableEntity> entities, CloudTable dataTable)
        {
            var batchOperation = new TableBatchOperation();
            entities.ToList().ForEach(entity => batchOperation.Insert(entity));
            dataTable.ExecuteBatch(batchOperation);
        }
    }
}
