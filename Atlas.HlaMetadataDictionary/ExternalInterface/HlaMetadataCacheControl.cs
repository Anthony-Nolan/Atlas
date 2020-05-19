using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;

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

        private readonly IAlleleNamesLookupRepository alleleNamesRepository;
        private readonly IHlaMatchingLookupRepository matchingLookupRepository;
        private readonly IHlaScoringLookupRepository scoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;

        public HlaMetadataCacheControl(
            string hlaNomenclatureVersion,

            IAlleleNamesLookupRepository alleleNamesRepository,
            IHlaMatchingLookupRepository matchingLookupRepository,
            IHlaScoringLookupRepository scoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository
        )
        {
            this.hlaNomenclatureVersion = hlaNomenclatureVersion;

            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingLookupRepository = matchingLookupRepository;
            this.scoringLookupRepository = scoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();

            await matchingLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await scoringLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
            await dpb1TceGroupsLookupRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(hlaNomenclatureVersion);
        }
    }
}