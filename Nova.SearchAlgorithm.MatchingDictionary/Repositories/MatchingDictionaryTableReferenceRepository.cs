using Nova.SearchAlgorithm.Common.Repositories;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    /// <summary>
    /// Holds the current table reference of the MatchingDictionary data table.
    /// </summary>
    public interface IMatchingDictionaryTableReferenceRepository
    {
        Task<string> GetCurrentMatchingDictionaryTableReference();
        string GetNewMatchingDictionaryTableReference();
        Task UpdateMatchingDictionaryTableReference(string dataTableReference);
    }

    public class MatchingDictionaryMatchingDictionaryTableReferenceRepository : IMatchingDictionaryTableReferenceRepository
    {
        private const string DataTableReferencePrefix = "MatchingDictionaryData";
        private readonly ITableReferenceRepository tableReferenceRepo;

        public MatchingDictionaryMatchingDictionaryTableReferenceRepository(ITableReferenceRepository tableReferenceRepo)
        {
            this.tableReferenceRepo = tableReferenceRepo;
        }

        public async Task<string> GetCurrentMatchingDictionaryTableReference()
        {
            return await tableReferenceRepo.GetCurrentTableReference(DataTableReferencePrefix);
        }

        public string GetNewMatchingDictionaryTableReference()
        {
            return tableReferenceRepo.GetNewTableReference(DataTableReferencePrefix);
        }

        public async Task UpdateMatchingDictionaryTableReference(string dataTableReference)
        {
            await tableReferenceRepo.UpdateTableReference(DataTableReferencePrefix, dataTableReference);
        }
    }
}
