using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.ApplicationInsights;
using Atlas.MatchingAlgorithm.Services.Debug;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    public class DonorFunctions
    {
        private readonly IHlaExpansionFailuresService hlaExpansionFailuresService;

        public DonorFunctions(IHlaExpansionFailuresService hlaExpansionFailuresService)
        {
            this.hlaExpansionFailuresService = hlaExpansionFailuresService;
        }

        [Function(nameof(HlaExpansionFailures))]
        [ProducesResponseType(typeof(IEnumerable<HlaExpansionFailure>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> HlaExpansionFailures(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(HlaExpansionFailures)}/" + "{daysToQuery?}"
                )]
            HttpRequest request,
            int? daysToQuery
        )
        {
            var output = await hlaExpansionFailuresService.Query(daysToQuery ?? 14);

            return new JsonResult(output);
        }
    }
}
