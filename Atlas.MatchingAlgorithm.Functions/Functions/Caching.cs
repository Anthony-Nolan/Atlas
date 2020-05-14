using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Atlas.HlaMetadataDictionary;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.Utils.CodeAnalysis;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class Caching
    {
        private readonly IAntigenCachingService antigenCachingService;
        private readonly IHlaMetadataCacheControl hlaMetadataCacheControl;

        public Caching(
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