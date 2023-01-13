using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    internal class FileBackedHlaScoringMetadataRepository :
        FileBackedHlaMetadataRepositoryBase<IHlaScoringMetadata>,
        IHlaScoringMetadataRepository
    {
        protected override IEnumerable<IHlaScoringMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection)
        {
            return metadataCollection.HlaScoringMetadata;
        }

        /// <inheritdoc />
        public Task<IDictionary<Locus, List<string>>> GetAllGGroups(string hlaNomenclatureVersion)
        {
            var gGroupsWithDuplicates = HlaMetadata[hlaNomenclatureVersion].Values
                .GroupBy(g => g.Locus)
                .Select(locusGroups => (locusGroups.Key, locusGroups.SelectMany(m => m.HlaScoringInfo.MatchingGGroups)));

            var gGroupsWithoutDuplicates = gGroupsWithDuplicates.ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.Item2.Where(x => x != null)).ToList()
            );

            return Task.FromResult((IDictionary<Locus, List<string>>) gGroupsWithoutDuplicates);
        }
    }
}