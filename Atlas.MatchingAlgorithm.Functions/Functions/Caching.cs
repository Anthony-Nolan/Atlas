using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class Caching
    {
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IHlaMatchingLookupRepository matchingLookupRepository;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IHlaScoringLookupRepository scoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public Caching(
            IAntigenCachingService antigenCachingService,
            IHlaMatchingLookupRepository matchingLookupRepository,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IHlaScoringLookupRepository scoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository, 
            IWmdaHlaVersionProvider wmdaHlaVersionProvider
        )
        {
            this.antigenCachingService = antigenCachingService;
            this.matchingLookupRepository = matchingLookupRepository;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.scoringLookupRepository = scoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        [FunctionName("UpdateHlaCache")]
        public async Task UpdateHlaCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await antigenCachingService.GenerateAntigenCache();
        }

        [FunctionName("UpdateMatchingDictionaryCache")]
        public async Task UpdateMatchingDictionaryCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            var hlaDatabaseVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();
            await matchingLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await alleleNamesLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await scoringLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
            await dpb1TceGroupsLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
        }
    }
}