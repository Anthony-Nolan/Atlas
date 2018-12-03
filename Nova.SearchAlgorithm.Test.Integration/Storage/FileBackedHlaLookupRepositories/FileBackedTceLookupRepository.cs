using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
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
