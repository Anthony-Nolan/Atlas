using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedGGroupToPGroupMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IMolecularTypingToPGroupMetadata>,
        IGGroupToPGroupMetadataRepository
    {
        protected override IEnumerable<IMolecularTypingToPGroupMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.GGroupToPGroupMetadata;
        }

        public Task<IMolecularTypingToPGroupMetadata> GetPGroupByGGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}
