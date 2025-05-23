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
        private readonly IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository;
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;
        private readonly ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository;

        public HlaMetadataCacheControl(
            string hlaNomenclatureVersion,
            IAlleleNamesMetadataRepository alleleNamesRepository,
            IHlaMatchingMetadataRepository matchingMetadataRepository,
            IHlaScoringMetadataRepository scoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository)
        {
            this.hlaNomenclatureVersion = hlaNomenclatureVersion;

            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingMetadataRepository = matchingMetadataRepository;
            this.scoringMetadataRepository = scoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
            this.hlaNameToSmallGGroupLookupRepository = hlaNameToSmallGGroupLookupRepository;
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
            this.serologyToAllelesMetadataRepository = serologyToAllelesMetadataRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();

            await matchingMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await scoringMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await dpb1TceGroupsMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await gGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await hlaNameToSmallGGroupLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await smallGGroupToPGroupMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await serologyToAllelesMetadataRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }
    }
}