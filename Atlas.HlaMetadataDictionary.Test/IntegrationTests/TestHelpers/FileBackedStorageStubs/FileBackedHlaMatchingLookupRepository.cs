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

        /// <param name="hlaNomenclatureVersion">
        /// The file backed version of the matching dictionary used for integration tests is locked
        /// to a single version of the HLA Nomenclature ("3330"), so this parameter is ignored.
        /// </param>
        public IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return HlaLookupResults.SelectMany(hla => hla.MatchingPGroups);
        }
    }
}
