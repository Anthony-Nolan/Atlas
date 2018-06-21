using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
    }

    public class MatchingDictionaryRepository : IMatchingDictionaryRepository
    {       
        private readonly ICloudTableFactory tableFactory;
        private readonly IMatchingDictionaryTableReferenceRepository matchingDictionaryTableReferenceRepository;

        public MatchingDictionaryRepository(ICloudTableFactory factory, IMatchingDictionaryTableReferenceRepository matchingDictionaryTableReferenceRepository)
        {
            tableFactory = factory;
            this.matchingDictionaryTableReferenceRepository = matchingDictionaryTableReferenceRepository;
        }

        public async Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            var newDataTable = CreateNewDataTable();
            InsertMatchingDictionaryEntriesIntoDataTable(dictionaryContents, newDataTable);
            await matchingDictionaryTableReferenceRepository.UpdateMatchingDictionaryTableReference(newDataTable.Name);
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {            
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var dataTable = await GetCurrentDataTable();
            var result = await dataTable.GetEntityByPartitionAndRowKey<MatchingDictionaryTableEntity>(partition, rowKey);

            return result?.ToMatchingDictionaryEntry();
        }

        private CloudTable CreateNewDataTable()
        {
            var dataTableReference = matchingDictionaryTableReferenceRepository.GetNewMatchingDictionaryTableReference();
            return tableFactory.GetTable(dataTableReference);
        }

        private async Task<CloudTable> GetCurrentDataTable()
        {
            var dataTableReference = await matchingDictionaryTableReferenceRepository.GetCurrentMatchingDictionaryTableReference();
            return tableFactory.GetTable(dataTableReference);
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
