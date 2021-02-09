using System.Collections.Generic;
using System.Linq;
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

        /// <inheritdoc />
        public Task<IDictionary<Locus, ISet<string>>> GetAllSmallGGroups(string hlaNomenclatureVersion)
        {
            var groups = HlaMetadata[hlaNomenclatureVersion].Values
                .GroupBy(x => x.Locus)
                .ToDictionary(x => x.Key, x => x.Select(x => x.LookupName).ToHashSet() as ISet<string>);

            return Task.FromResult((IDictionary<Locus, ISet<string>>) groups);
        }

        public Task<IMolecularTypingToPGroupMetadata> GetPGroupBySmallGGroupIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}