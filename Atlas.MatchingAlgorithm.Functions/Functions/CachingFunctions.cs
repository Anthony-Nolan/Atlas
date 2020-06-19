using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class CachingFunctions
    {
        private readonly IHlaMetadataCacheControl hlaMetadataCacheControl;
        private readonly IMacDictionary macDictionary;

        public CachingFunctions(IHlaMetadataCacheControl hlaMetadataCacheControl, IMacDictionary macDictionary)
        {
            this.hlaMetadataCacheControl = hlaMetadataCacheControl;
            this.macDictionary = macDictionary;
        }
        
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(UpdateMacDictionaryCache))]
        public async Task UpdateMacDictionaryCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await macDictionary.GenerateMacCache();
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