using System.Collections.Generic;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    public class FileBackedHlaScoringLookupRepository :
        FileBackedHlaLookupRepositoryBase<IHlaScoringLookupResult>,
        IHlaScoringLookupRepository
    {
        protected override IEnumerable<IHlaScoringLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.HlaScoringLookupResults;
        }
    }
}
