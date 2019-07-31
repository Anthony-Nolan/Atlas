using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Services.DataRefresh;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class DataRefresh
    {
        private readonly IDataRefreshOrchestrator dataRefreshOrchestrator;

        public DataRefresh(IDataRefreshOrchestrator dataRefreshOrchestrator)
        {
            this.dataRefreshOrchestrator = dataRefreshOrchestrator;
        }

        /// <summary>
        /// Runs a full data refresh, regardless of whether the existing database is using the latest version of the hla database
        /// </summary>
        [FunctionName("ForceDataRefresh")]
        public async Task ForceDataRefresh([HttpTrigger] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);
        }

        /// <summary>
        /// Runs a full data refresh, if necessary
        /// </summary>
        [FunctionName("RunDataRefreshManual")]
        public async Task RunDataRefreshManual([HttpTrigger] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }

        /// <summary>
        /// Runs a full data refresh, without clearing out old data - can be used when an in progress job failed near the end of the job,
        /// To avoid importing / processing donors more than necessary
        /// </summary>
        [FunctionName("ContinueDataRefreshManual")]
        public async Task ContinueDataRefreshManual([HttpTrigger] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary(true);
        }

        /// <summary>
        /// Runs a full data refresh, if necessary
        /// </summary>
        [FunctionName("RunDataRefresh")]
        public async Task RunDataRefresh([TimerTrigger("%DataRefresh.CronTab%")] TimerInfo timerInfo)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }
    }
}