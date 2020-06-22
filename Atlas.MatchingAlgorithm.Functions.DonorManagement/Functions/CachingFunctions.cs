using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class CachingFunctions
    {
        private readonly IHlaMetadataCacheControl hlaMetadataCacheControl;
        private readonly IMacDictionary macDictionary;

        public CachingFunctions(
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IMacDictionary macDictionary
        )
        {
            hlaMetadataCacheControl = hlaMetadataDictionaryFactory.BuildCacheControl(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
            this.macDictionary = macDictionary;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(UpdateHlaMetadataDictionaryCache))]
        public async Task UpdateHlaMetadataDictionaryCache(
            [TimerTrigger("00 00 03 * * *", RunOnStartup = true)]
            TimerInfo timerInfo)
        {
            await hlaMetadataCacheControl.PreWarmAlleleNameCache();
        }
    }
}