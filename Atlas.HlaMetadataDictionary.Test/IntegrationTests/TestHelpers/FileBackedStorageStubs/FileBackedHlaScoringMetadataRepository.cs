using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

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
