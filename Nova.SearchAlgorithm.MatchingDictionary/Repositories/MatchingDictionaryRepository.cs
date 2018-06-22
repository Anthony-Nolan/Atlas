using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        Task ConnectToCloudTable();
    }

    public class MatchingDictionaryRepository : IMatchingDictionaryRepository
    {       
        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private CloudTable cloudTable;

        public MatchingDictionaryRepository(ICloudTableFactory factory, ITableReferenceRepository tableReferenceRepository)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
        }

        public async Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            var newDataTable = CreateNewDataTable();
            InsertMatchingDictionaryEntriesIntoDataTable(dictionaryContents, newDataTable);
            await tableReferenceRepository.UpdateMatchingDictionaryTableReference(newDataTable.Name);
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {            
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var dataTable = await GetCurrentDataTable();
            var result = await dataTable.GetEntityByPartitionAndRowKey<MatchingDictionaryTableEntity>(partition, rowKey);

            return result?.ToMatchingDictionaryEntry();
        }

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// As the lookups in the HLA Refresh job are asynchronous, we need a way of populating this cache synchronously up front
        /// If you plan to use this repository with multiple async operations, this method should be called first
        /// </summary>
        public async Task ConnectToCloudTable()
        {
            await GetCurrentDataTable();
        }

        private CloudTable CreateNewDataTable()
        {
            var dataTableReference = tableReferenceRepository.GetNewMatchingDictionaryTableReference();
            return tableFactory.GetOrCreateTable(dataTableReference);
        }

        private async Task<CloudTable> GetCurrentDataTable()
        {
            if (cloudTable == null)
            {
                var dataTableReference = await tableReferenceRepository.GetCurrentMatchingDictionaryTableReference();
                cloudTable = tableFactory.GetOrCreateTable(dataTableReference);
            }
            return cloudTable;
        }

        private static void InsertMatchingDictionaryEntriesIntoDataTable(IEnumerable<MatchingDictionaryEntry> contents, CloudTable dataTable)
        {
            var contentsList = contents.ToList();

            foreach (var partition in PermittedLocusNames.GetPermittedMatchLoci())
            {
                var partitionEntities = contentsList
                    .Where(entry => entry.MatchLocus.Equals(partition))
                    .Select(entry => entry.ToTableEntity());

                dataTable.BatchInsert(partitionEntities);
            }
        }        
    }
}
