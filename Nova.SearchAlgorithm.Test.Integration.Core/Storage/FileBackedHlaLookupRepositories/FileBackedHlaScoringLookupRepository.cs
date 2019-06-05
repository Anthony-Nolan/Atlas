using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
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
