using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.Utils.CodeAnalysis;

//QQ These endpoints should remain here, but the version fetch gets pushed into the HlaMetadataDictionary class?
namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class Caching
    {
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IActiveHlaVersionAccessor hlaVersionProvider;

        public Caching(
            IAntigenCachingService antigenCachingService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IActiveHlaVersionAccessor hlaVersionProvider
        )
        {
            this.antigenCachingService = antigenCachingService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.hlaVersionProvider = hlaVersionProvider;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("UpdateHlaCache")]
        public async Task UpdateHlaCache(
            [TimerTrigger("00 00 03 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await antigenCachingService.GenerateAntigenCache();
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("UpdateMatchingDictionaryCache")]
        public async Task UpdateMatchingDictionaryCache(
            [TimerTrigger("00 00 03 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            var hlaDatabaseVersion = hlaVersionProvider.GetActiveHlaDatabaseVersion();
            await alleleNamesLookupRepository.LoadDataIntoMemory(hlaDatabaseVersion);
        }
    }
}