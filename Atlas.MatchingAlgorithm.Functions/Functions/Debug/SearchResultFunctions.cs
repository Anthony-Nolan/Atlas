using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;
using System.Net;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class SearchResultFunctions
    {
        private readonly AzureStorageSettings azureStorageSettings;

        public SearchResultFunctions(IOptions<AzureStorageSettings> azureStorageSettings)
        {
            this.azureStorageSettings = azureStorageSettings.Value;
        }

        [FunctionName(nameof(AreResultsBatched))]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public IActionResult AreResultsBatched(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = $"{RouteConstants.DebugRoutePrefix}/searchResults/areBatched")]
            HttpRequest request)
        {
            return new JsonResult(azureStorageSettings.ShouldBatchResults);
        }
    }
}