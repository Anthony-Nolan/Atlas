using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    /// <summary>
    /// Health checks for the API.
    /// Currently only provides a minimal "is the API running" check.
    /// Other checks could be added in the future (e.g. "Can I see the database?" "Can I talk to all the APIs I care about?", etc.)
    /// </summary>
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        [Route("api-check")]
        public ActionResult Check()
        {
            return Ok("API is Running. No other checks performed.");
        }
    }
}