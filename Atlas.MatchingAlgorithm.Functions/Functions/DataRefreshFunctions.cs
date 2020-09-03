using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class DataRefreshFunctions
    {
        private readonly IDataRefreshOrchestrator dataRefreshOrchestrator;
        private readonly IDataRefreshCleanupService dataRefreshCleanupService;

        public DataRefreshFunctions(IDataRefreshOrchestrator dataRefreshOrchestrator, IDataRefreshCleanupService dataRefreshCleanupService)
        {
            this.dataRefreshOrchestrator = dataRefreshOrchestrator;
            this.dataRefreshCleanupService = dataRefreshCleanupService;
        }

        /// <summary>
        /// Runs a full data refresh, regardless of whether the existing metadata dictionary is using the latest version of hla nomenclature.
        /// If the nomenclature is up to date, a metadata dictionary refresh will not be performed.
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ForceDataRefresh))]
        public async Task ForceDataRefresh([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);
        }

        /// <summary>
        /// Runs a full data refresh, if necessary
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunDataRefreshManual))]
        public async Task RunDataRefreshManual([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }
        
        /// <summary>
        /// If a single data refresh job is in progress, continues from the last successful stage.
        /// Should not be called unless you are sure that an in progress job has been interrupted, to avoid multiple jobs running concurrently.
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ContinueDataRefresh))]
        public async Task ContinueDataRefresh([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await dataRefreshOrchestrator.ContinueDataRefresh();
        }

        /// <summary>
        /// Runs a full data refresh, if necessary
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunDataRefresh))]
        public async Task RunDataRefresh([TimerTrigger("%DataRefresh:CronTab%")] TimerInfo timerInfo)
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();
        }

        /// <summary>
        /// Manually triggers cleanup after the data refresh.
        /// This clean up covers scaling down the database that was scaled up for the refresh, and re-enabling donor update functions.
        /// Clean up should have been run if the job completed, whether successfully or not.
        /// The only time this should be triggered is if the server running the data refresh was restarted while the job was in progress, causing it to skip tear-down.
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunDataRefreshCleanup))]
        public async Task RunDataRefreshCleanup([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            await dataRefreshCleanupService.RunDataRefreshCleanup();
        }
    }
}