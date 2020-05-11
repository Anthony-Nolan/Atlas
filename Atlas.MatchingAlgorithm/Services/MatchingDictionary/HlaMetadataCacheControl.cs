using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.MatchingDictionary
{
    public interface IHlaMetadataCacheControl
    {
        /// <summary>
        /// Force all in-memory caches to populate (used to achieve slow-start-up, rather than slow-first-request)
        /// </summary>
        /// <remarks>
        /// Currently only populates the AlleleNameRepository. Other caches might need to be populated. TBC.
        /// </remarks>
        Task PreWarmAllCaches();

        /// <summary>
        /// Force population of the in-memory cache of AlleleNames. QQ better description?
        /// </summary>
        /// <remarks>
        /// Currently only populates the AlleleNameRepository. Other caches might need to be populated. TBC.
        /// </remarks>
        Task PreWarmAlleleNameCache();
    }

    //QQ Migrate to HlaMdDictionary.
    public class HlaMetadataCacheControl : IHlaMetadataCacheControl
    {
        private readonly IActiveHlaVersionAccessor activeHlaVersionProvider;

        private readonly IAlleleNamesLookupRepository alleleNamesRepository;
        private readonly IHlaMatchingLookupRepository matchingLookupRepository;
        private readonly IHlaScoringLookupRepository scoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;

        public HlaMetadataCacheControl(
            IActiveHlaVersionAccessor activeHlaVersionProvider,

            IAlleleNamesLookupRepository alleleNamesRepository,
            IHlaMatchingLookupRepository matchingLookupRepository,
            IHlaScoringLookupRepository scoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository
        )
        {
            this.activeHlaVersionProvider = activeHlaVersionProvider;//QQ This will be replaced by the value being passed in directly. How does hot swapping work?

            this.alleleNamesRepository = alleleNamesRepository;
            this.matchingLookupRepository = matchingLookupRepository;
            this.scoringLookupRepository = scoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
        }

        public async Task PreWarmAllCaches()
        {
            await PreWarmAlleleNameCache();

            var hlaDatabaseVersion = activeHlaVersionProvider.GetActiveHlaDatabaseVersion();

            await matchingLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await scoringLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await dpb1TceGroupsLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            //QQ consider other caches that deserve warming.
        }

        public async Task PreWarmAlleleNameCache()
        {
            await alleleNamesRepository.LoadDataIntoMemory(activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
            //QQ consider other caches that deserve warming.
        }
    }
}