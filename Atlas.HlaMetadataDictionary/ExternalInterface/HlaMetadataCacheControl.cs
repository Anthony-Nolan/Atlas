using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public interface IHlaMetadataCacheControl
    {
        /// <summary>
        /// Force all in-memory caches to populate (used to achieve slow-start-up, rather than slow-first-request)
        /// </summary>
        Task PreWarmAllCaches();

        /// <summary>
        /// Force population of the in-memory cache of AlleleNames.
        /// </summary>
        Task PreWarmAlleleNameCache();
    }

    internal class HlaMetadataCacheControl : IHlaMetadataCacheControl
    {
        private readonly string hlaNomenclatureVersion;

        private readonly IAlleleNamesMetadataRepository alleleNamesRepository;
        private readonly IHlaMatchingMetadataRepository matchingMetadataRepository;
        private readonly IHlaScoringMetadataRepository scoringMetadataRepository;
        private readonly IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository;
        private readonly IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository;
        private readonly ISmallGGroupsMetadataRepository smallGGroupsMetadataRepository;
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;

        public HlaMetadataCacheControl(
            string hlaNomenclatureVersion,
            IAlleleNamesMetadataRepository alleleNamesRepository,
            IHlaMatchingMetadataRepository matchingMetadataRepository,
            IHlaScoringMetadataRepository scoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            ISmallGGroupsMetadataRepository smallGGroupsMetadataRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository)
        {
            this.hlaNomenclatureVersion = hlaNomenclatureVersion;

            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingMetadataRepository = matchingMetadataRepository;
            this.scoringMetadataRepository = scoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
            this.smallGGroupsMetadataRepository = smallGGroupsMetadataRepository;
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();

            await matchingMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await scoringMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await dpb1TceGroupsMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await gGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await smallGGroupsMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await smallGGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }
    }
}