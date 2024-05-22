using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class HlaMetadataDictionaryFunctions //TODO: ATLAS-262 (MDM) migrate to new project
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryFunctions(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            hlaMetadataDictionary = factory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion());
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RefreshHlaMetadataDictionary))]
        public async Task RefreshHlaMetadataDictionary([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest);
        }

        /// <remarks>
        /// Normally our client models live in a dedicated project ... but this isn't really a client model.
        /// It only exists because Microsoft haven't provided nice model binding (or even primitive parameter binding) in Function declarations.
        /// Further, this endpoint isn't going to be hit by an external integration - it's only ever going to be used by devs,
        /// via Swagger or PostMan, or similar.
        /// </remarks>
        public class VersionRequest
        {
            public string Version { get; set; }
        }
        
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RefreshHlaMetadataDictionaryToSpecificVersion))]
        public async Task RefreshHlaMetadataDictionaryToSpecificVersion(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(VersionRequest), nameof(VersionRequest))]
            HttpRequest httpRequest)
        {
            var version = JsonConvert.DeserializeObject<VersionRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync()).Version;
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Specific(version));
        }
    }
}