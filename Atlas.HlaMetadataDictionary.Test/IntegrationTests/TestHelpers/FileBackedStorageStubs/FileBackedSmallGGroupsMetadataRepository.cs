using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedSmallGroupsMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<ISmallGGroupsMetadata>,
        ISmallGGroupsMetadataRepository
    {
        protected override IEnumerable<ISmallGGroupsMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.SmallGGroupMetadata;
        }
    }
}
