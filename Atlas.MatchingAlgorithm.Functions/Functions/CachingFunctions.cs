using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class CachingFunctions
    {
        private readonly IHlaMetadataCacheControl hlaMetadataCacheControl;

        public CachingFunctions(
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor
        )
        {
            try
            {
                var activeHlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
                hlaMetadataCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(activeHlaNomenclatureVersion);
            }
            catch (ArgumentNullException)
            {
                //No active version is defined. No cache warming is necessary.
                hlaMetadataCacheControl = null;
            }
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(UpdateHlaMetadataDictionaryCache))]
        public async Task UpdateHlaMetadataDictionaryCache(
            [TimerTrigger("00 00 02 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await (hlaMetadataCacheControl?.PreWarmAlleleNameCache() ?? Task.CompletedTask);
        }
    }
}