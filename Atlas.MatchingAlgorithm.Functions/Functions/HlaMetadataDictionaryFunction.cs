using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class HlaMetadataDictionaryFunction //TODO: ATLAS-262 (MDM) migrate to new project
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryFunction(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaVersionAccessor hlaVersionAccessor)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaVersionAccessor.GetActiveHlaDatabaseVersion());
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("RefreshHlaMetadataDictionary")]
        public async Task Refresh([HttpTrigger] HttpRequest httpRequest)
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
        }
    }
}