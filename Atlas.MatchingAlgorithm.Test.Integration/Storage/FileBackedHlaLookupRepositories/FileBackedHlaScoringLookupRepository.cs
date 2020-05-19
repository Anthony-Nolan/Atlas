using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    internal class FileBackedHlaScoringLookupRepository :
        FileBackedHlaLookupRepositoryBase<IHlaScoringLookupResult>,
        IHlaScoringLookupRepository
    {
        protected override IEnumerable<IHlaScoringLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections)
        {
            return resultCollections.HlaScoringLookupResults;
        }
    }
}
