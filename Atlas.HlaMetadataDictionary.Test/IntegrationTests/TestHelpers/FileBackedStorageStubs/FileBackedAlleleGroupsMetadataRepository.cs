using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedAlleleGroupsMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IAlleleGroupMetadata>,
        IAlleleGroupsMetadataRepository
    {
        protected override IEnumerable<IAlleleGroupMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.AlleleGroupMetadata;
        }

        public Task<IAlleleGroupMetadata> GetAlleleGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}
