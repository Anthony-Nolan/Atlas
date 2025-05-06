using Atlas.SearchTracking.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.SearchTracking.Functions.Functions
{
    public class SearchTrackingDebugFunctions
    {
        private readonly ISearchTrackingDebugService searchTrackingDebugService;

        public SearchTrackingDebugFunctions(ISearchTrackingDebugService searchTrackingDebugService)
        {
            this.searchTrackingDebugService = searchTrackingDebugService;
        }

        [Function(nameof(GetSearchRequestByIdentifier))]
        public async Task<IActionResult> GetSearchRequestByIdentifier(
            [HttpTrigger(AuthorizationLevel.Function, "get")]
            HttpRequest request)
        {
            var searchIdentifier = Guid.Parse(request.Query["searchIdentifier"]);
            var searchRequest = await searchTrackingDebugService.GetSearchRequestByIdentifier(searchIdentifier);

            return new JsonResult(searchRequest);
        }
    }
}
