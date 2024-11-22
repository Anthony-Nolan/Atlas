using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using System;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class HealthCheckFunctions
    {
        private readonly HealthCheckService healthCheckService;

        public HealthCheckFunctions(HealthCheckService healthCheckService)
        {
            this.healthCheckService = healthCheckService;
        }

        [Function(nameof(HealthCheck))]
        public async Task<IActionResult> HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request)
        {
            var healthStatus = await healthCheckService.CheckHealthAsync();
            return new JsonResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
        }
    }
}
