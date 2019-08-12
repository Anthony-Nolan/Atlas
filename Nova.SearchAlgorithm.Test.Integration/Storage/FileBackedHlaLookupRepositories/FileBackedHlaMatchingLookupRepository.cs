using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedHlaMatchingLookupRepository :
        FileBackedHlaLookupRepositoryBase<IHlaMatchingLookupResult>,
        IHlaMatchingLookupRepository
    {
        protected override IEnumerable<IHlaMatchingLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.HlaMatchingLookupResults;
        }

        public IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion)
        {
            return HlaLookupResults.SelectMany(hla => hla.MatchingPGroups);
        }
    }
}
