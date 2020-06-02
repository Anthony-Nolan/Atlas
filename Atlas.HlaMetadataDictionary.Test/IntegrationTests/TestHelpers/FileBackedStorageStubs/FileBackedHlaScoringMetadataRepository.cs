using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedHlaScoringMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IHlaScoringMetadata>,
        IHlaScoringMetadataRepository
    {
        protected override IEnumerable<IHlaScoringMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.HlaScoringMetadata;
        }
    }
}
