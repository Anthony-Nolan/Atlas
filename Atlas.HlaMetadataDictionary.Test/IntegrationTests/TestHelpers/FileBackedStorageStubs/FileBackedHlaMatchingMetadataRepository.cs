using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedHlaMatchingMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IHlaMatchingMetadata>,
        IHlaMatchingMetadataRepository
    {
        protected override IEnumerable<IHlaMatchingMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.HlaMatchingMetadata;
        }

        public IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return HlaMetadata[hlaNomenclatureVersion].Values.SelectMany(m => m.MatchingPGroups);
        }
    }
}