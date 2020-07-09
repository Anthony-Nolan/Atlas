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

        /// <param name="hlaNomenclatureVersion">
        /// The file backed version of the Metadata Dictionary used for integration tests is locked
        /// to a single version of the HLA Nomenclature ("3330"), so this parameter is ignored.
        /// </param>
        public IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return HlaMetadata.Values.SelectMany(m => m.MatchingPGroups);
        }
    }
}