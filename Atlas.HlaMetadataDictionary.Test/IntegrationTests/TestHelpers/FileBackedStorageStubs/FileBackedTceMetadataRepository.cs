using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedTceMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IDpb1TceGroupsMetadata>,
        IDpb1TceGroupsMetadataRepository
    {
        protected override IEnumerable<IDpb1TceGroupsMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.Dpb1TceGroupMetadata;
        }
    }
}
