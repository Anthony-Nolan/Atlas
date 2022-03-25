using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.Functions.Functions
{
    public class MonitoringFunctions
    {
        private readonly IMonitoringService monitoringService;

        public MonitoringFunctions(IMonitoringService monitoringService)
        {
            this.monitoringService = monitoringService;
        }

        [FunctionName(nameof(GetRunningInstances))]
        public async Task<List<SearchRequestTrackingInfo>> GetRunningInstances(
            [HttpTrigger(AuthorizationLevel.Admin)] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient client
            )
        {
            return await monitoringService.GetAllOngoingSearches(client);
        }
    }
}