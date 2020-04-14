using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedTceLookupRepository :
        FileBackedHlaLookupRepositoryBase<IDpb1TceGroupsLookupResult>,
        IDpb1TceGroupsLookupRepository
    {
        protected override IEnumerable<IDpb1TceGroupsLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.Dpb1TceGroupLookupResults;
        }
    }
}
