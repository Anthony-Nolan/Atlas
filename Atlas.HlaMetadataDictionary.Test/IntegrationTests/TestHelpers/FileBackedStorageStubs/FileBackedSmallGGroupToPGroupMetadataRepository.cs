using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedSmallGGroupToPGroupMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IMolecularTypingToPGroupMetadata>,
        ISmallGGroupToPGroupMetadataRepository
    {
        protected override IEnumerable<IMolecularTypingToPGroupMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.SmallGGroupToPGroupMetadata;
        }

        public Task<IMolecularTypingToPGroupMetadata> GetPGroupBySmallGGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}