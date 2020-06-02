using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

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
