using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class CachingFunctions
    {
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IHlaMetadataCacheControl hlaMetadataCacheControl;

        public CachingFunctions(
            IAntigenCachingService antigenCachingService,
            IHlaMetadataCacheControl hlaMetadataCacheControl
        )
        {
            this.antigenCachingService = antigenCachingService;
            this.hlaMetadataCacheControl = hlaMetadataCacheControl;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(UpdateHlaCache))]
        public async Task UpdateHlaCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await antigenCachingService.GenerateAntigenCache();
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(UpdateHlaMetadataDictionaryCache))]
        public async Task UpdateHlaMetadataDictionaryCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await hlaMetadataCacheControl.PreWarmAllCaches();
        }
    }
}