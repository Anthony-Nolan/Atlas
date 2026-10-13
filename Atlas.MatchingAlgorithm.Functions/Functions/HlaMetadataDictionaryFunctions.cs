using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class HlaMetadataDictionaryFunctions //TODO: ATLAS-262 (MDM) migrate to new project
    {
        private const string DataRefreshInProgressMessage =
            "A data refresh appears to be in progress, and it recreates the HLA Metadata Dictionary itself as its first " +
            "stage, so the dictionary was not recreated. Try again once the refresh has completed. A stalled refresh also " +
            "counts as in progress, until the stalled-refresh watchdog recovers it.";

        private readonly IManualHlaMetadataDictionaryRefresher refresher;

        public HlaMetadataDictionaryFunctions(IManualHlaMetadataDictionaryRefresher refresher)
        {
            this.refresher = refresher;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RefreshHlaMetadataDictionary))]
        public async Task<IActionResult> RefreshHlaMetadataDictionary([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            return await Recreate(CreationBehaviour.Latest);
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
        public async Task<IActionResult> RefreshHlaMetadataDictionaryToSpecificVersion(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(VersionRequest), nameof(VersionRequest))]
            HttpRequest httpRequest)
        {
            var version = JsonConvert.DeserializeObject<VersionRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync()).Version;
            return await Recreate(CreationBehaviour.Specific(version));
        }

        /// <remarks>
        /// A 400 rather than a thrown exception, matching <c>SubmitDataRefreshRequestManual</c>: nothing in this app maps
        /// an exception to a status code, so throwing would surface to the caller as an unexplained 500.
        /// </remarks>
        private async Task<IActionResult> Recreate(CreationBehaviour creationBehaviour) =>
            await refresher.TryRecreate(creationBehaviour)
                ? new OkResult()
                : new BadRequestObjectResult(DataRefreshInProgressMessage);
    }
}