using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class DataRefreshFunctions
    {
        private readonly IDataRefreshRequester dataRefreshRequester;
        private readonly IDataRefreshOrchestrator dataRefreshOrchestrator;
        private readonly IDataRefreshCleanupService dataRefreshCleanupService;

        public DataRefreshFunctions(
            IDataRefreshRequester dataRefreshRequester,
            IDataRefreshOrchestrator dataRefreshOrchestrator,
            IDataRefreshCleanupService dataRefreshCleanupService)
        {
            this.dataRefreshRequester = dataRefreshRequester;
            this.dataRefreshOrchestrator = dataRefreshOrchestrator;
            this.dataRefreshCleanupService = dataRefreshCleanupService;
        }

        /// <summary>
        /// Requests a data refresh according to submitted request parameters.
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(SubmitDataRefreshRequestManual))]
        public async Task<IActionResult> SubmitDataRefreshRequestManual(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DataRefreshRequest), nameof(DataRefreshRequest))]
            HttpRequest httpRequest)
        {
            try
            {
                var request = await ReadRequestBody<DataRefreshRequest>(httpRequest);
                var recordId = await dataRefreshRequester.RequestDataRefresh(request, true);
                return new JsonResult(recordId);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        /// <summary>
        /// Requests a full data refresh, if necessary.
        /// </summary>
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(SubmitDataRefreshRequest))]
        public async Task SubmitDataRefreshRequest([TimerTrigger("%DataRefresh:CronTab%")] TimerInfo timerInfo)
        {
            var request = new DataRefreshRequest { ForceDataRefresh = false };
            await dataRefreshRequester.RequestDataRefresh(request, false);
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(RunDataRefresh))]
        public async Task RunDataRefresh(
            [ServiceBusTrigger(
                "%DataRefresh:RequestsTopic%",
                "%DataRefresh:RequestsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            ValidatedDataRefreshRequest request)
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(request.DataRefreshRecordId);
        }

        [FunctionName(nameof(DataRefreshDeadLetterQueueListener))]
        public async Task DataRefreshDeadLetterQueueListener(
            [ServiceBusTrigger(
                "%DataRefresh:RequestsTopic%/Subscriptions/%DataRefresh:RequestsTopicSubscription%/$DeadLetterQueue",
                "%DataRefresh:RequestsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString")]
            ValidatedDataRefreshRequest request)
        {
            await dataRefreshCleanupService.RunDataRefreshCleanup();
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

        private static async Task<T> ReadRequestBody<T>(HttpRequest request)
        {
            return JsonConvert.DeserializeObject<T>(await new StreamReader(request.Body).ReadToEndAsync());
        }
    }
}