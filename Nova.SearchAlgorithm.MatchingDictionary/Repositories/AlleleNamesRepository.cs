using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IAlleleNamesRepository
    {
        Task RecreateAlleleNamesTable(IEnumerable<AlleleNameEntry> alleleNames);
        Task<AlleleNameEntry> GetAlleleNameIfExists(MatchLocus matchLocus, string lookupName);
        Task LoadAlleleNamesIntoMemory();
    }

    public class AlleleNamesRepository :
        LookupRepositoryBase<AlleleNameEntry, AlleleNameTableEntity>,
        IAlleleNamesRepository
    {
        private const string DataTableReferencePrefix = "AlleleNamesData";
        private const string CacheKeyAlleleNames = "AlleleNames";

        public AlleleNamesRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKeyAlleleNames)
        {
        }

        public async Task RecreateAlleleNamesTable(IEnumerable<AlleleNameEntry> alleleNames)
        {
            var partitions = GetTablePartitions();
            await RecreateDataTable(alleleNames, partitions);
        }

        public async Task<AlleleNameEntry> GetAlleleNameIfExists(MatchLocus matchLocus, string lookupName)
        {
            var partition = AlleleNameTableEntity.GetPartition(matchLocus);
            var rowKey = AlleleNameTableEntity.GetRowKey(lookupName);
            var entity = await GetDataIfExists(partition, rowKey);

            return entity?.ToAlleleNameEntry();
        }

        public async Task LoadAlleleNamesIntoMemory()
        {
            await LoadDataIntoMemory();
        }

        protected override IEnumerable<string> GetTablePartitions()
        {
            return PermittedLocusNames
                .GetPermittedMatchLoci()
                .Select(matchLocus => matchLocus.ToString());
        }
    }
}
