using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedSerologyToAllelesMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<ISerologyToAllelesMetadata>,
        ISerologyToAllelesMetadataRepository
    {
        protected override IEnumerable<ISerologyToAllelesMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.SerologyToAllelesMetadata;
        }
    }
}
