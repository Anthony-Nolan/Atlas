using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Repositories;

namespace Atlas.MatchingAlgorithm.Services.MatchingDictionary
{
    public interface IHlaMetadataCacheControl
    {
        /// <summary>
        /// Force all in-memory caches to populate (used to achieve slow-start-up, rather than slow-first-request)
        /// </summary>
        Task PreWarmAllCaches();

        /// <summary>
        /// Force population of the in-memory cache of AlleleNames. QQ better description?
        /// </summary>
        Task PreWarmAlleleNameCache();
    }

    //QQ Migrate to HlaMdDictionary.
    public class HlaMetadataCacheControl : IHlaMetadataCacheControl
    {
        private readonly HlaMetadataConfiguration config;

        private readonly IAlleleNamesLookupRepository alleleNamesRepository;
        private readonly IHlaMatchingLookupRepository matchingLookupRepository;
        private readonly IHlaScoringLookupRepository scoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;

        public HlaMetadataCacheControl(
            HlaMetadataConfiguration config,

            IAlleleNamesLookupRepository alleleNamesRepository,
            IHlaMatchingLookupRepository matchingLookupRepository,
            IHlaScoringLookupRepository scoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository
        )
        {
            this.config = config;

            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingLookupRepository = matchingLookupRepository;
            this.scoringLookupRepository = scoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();
            var hlaDatabaseVersion = config.ActiveWmdaVersion; //QQ actually needs to pass the whole object

            await matchingLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await scoringLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await dpb1TceGroupsLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(config.ActiveWmdaVersion); //QQ actually needs to pass the whole object
        }
    }
}