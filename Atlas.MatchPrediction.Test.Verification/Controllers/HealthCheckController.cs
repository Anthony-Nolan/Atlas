using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchPrediction.Test.Verification.Controllers
{
    /// <summary>
    /// Health checks for the API.
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