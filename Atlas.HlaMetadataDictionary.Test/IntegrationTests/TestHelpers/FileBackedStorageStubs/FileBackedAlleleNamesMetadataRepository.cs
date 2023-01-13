using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedAlleleNamesMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IAlleleNameMetadata>,
        IAlleleNamesMetadataRepository
    {
        protected override IEnumerable<IAlleleNameMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.AlleleNameMetadata;
        }

        public Task<IAlleleNameMetadata> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}
