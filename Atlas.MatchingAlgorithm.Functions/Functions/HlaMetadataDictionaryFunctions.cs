using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class HlaMetadataDictionaryFunctions //TODO: ATLAS-262 (MDM) migrate to new project
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryFunctions(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaVersionAccessor hlaVersionAccessor)
        {
            hlaMetadataDictionary = factory.BuildDictionary(hlaVersionAccessor.GetActiveHlaDatabaseVersion());
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RefreshHlaMetadataDictionary))]
        public async Task RefreshHlaMetadataDictionary([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
        }

        public class VersionRequest
        {
            public string Version { get; set; }
        }
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RefreshHlaMetadataDictionaryToSpecificVersion))]
        public async Task RefreshHlaMetadataDictionaryToSpecificVersion([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            var version = JsonConvert.DeserializeObject<VersionRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync()).Version;
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(version));
        }

    }
}