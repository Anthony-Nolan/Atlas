using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class Caching
    {
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public Caching(
            IAntigenCachingService antigenCachingService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository, 
            IWmdaHlaVersionProvider wmdaHlaVersionProvider
        )
        {
            this.antigenCachingService = antigenCachingService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        [FunctionName("UpdateHlaCache")]
        public async Task UpdateHlaCache(
            [TimerTrigger("00 00 03 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await antigenCachingService.GenerateAntigenCache();
        }

        [FunctionName("UpdateMatchingDictionaryCache")]
        public async Task UpdateMatchingDictionaryCache(
            [TimerTrigger("00 00 03 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            var hlaDatabaseVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();
            await alleleNamesLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
        }
    }
}