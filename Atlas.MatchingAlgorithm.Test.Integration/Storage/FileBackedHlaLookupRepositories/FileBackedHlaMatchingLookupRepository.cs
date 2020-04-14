using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedHlaMatchingLookupRepository :
        FileBackedHlaLookupRepositoryBase<IHlaMatchingLookupResult>,
        IHlaMatchingLookupRepository
    {
        protected override IEnumerable<IHlaMatchingLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.HlaMatchingLookupResults;
        }

        /// <param name="hlaDatabaseVersion">The file backed version of the matching dictionary used for integration tests does not
        /// support multiple versions of the hla database, so this parameter is ignored</param>
        public IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion)
        {
            return HlaLookupResults.SelectMany(hla => hla.MatchingPGroups);
        }
    }
}
