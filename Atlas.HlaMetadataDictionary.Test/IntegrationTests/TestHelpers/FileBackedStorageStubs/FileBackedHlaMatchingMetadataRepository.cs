using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public Task<IEnumerable<string>> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return Task.FromResult(HlaMetadata[hlaNomenclatureVersion].Values.SelectMany(m => m.MatchingPGroups));
        }
    }
}