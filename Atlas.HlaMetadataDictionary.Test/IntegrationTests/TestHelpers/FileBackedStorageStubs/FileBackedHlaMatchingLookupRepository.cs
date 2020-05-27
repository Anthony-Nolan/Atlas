using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedHlaMatchingLookupRepository :
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
