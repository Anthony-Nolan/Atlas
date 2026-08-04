using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Azure.Messaging.ServiceBus;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class DataRefreshFunctions
    {
        private readonly IDataRefreshRequester dataRefreshRequester;
        private readonly IDataRefreshOrchestrator dataRefreshOrchestrator;
        private readonly IDataRefreshCleanupService dataRefreshCleanupService;
        private readonly ILogger<DataRefreshFunctions> logger;

        public DataRefreshFunctions(
            IDataRefreshRequester dataRefreshRequester,
            IDataRefreshOrchestrator dataRefreshOrchestrator,
            IDataRefreshCleanupService dataRefreshCleanupService,
            ILogger<DataRefreshFunctions> logger)
        {
            this.dataRefreshRequester = dataRefreshRequester;
            this.dataRefreshOrchestrator = dataRefreshOrchestrator;
            this.dataRefreshCleanupService = dataRefreshCleanupService;
            this.logger = logger;
        }

        /// <summary>
        /// Requests a data refresh according to submitted request parameters.
        /// </summary>
        [Function(nameof(SubmitDataRefreshRequestManual))]
        public async Task<IActionResult> SubmitDataRefreshRequestManual(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DataRefreshRequest), nameof(DataRefreshRequest))]
            HttpRequest httpRequest)
        {
            try
            {
                var request = await ReadRequestBody<DataRefreshRequest>(httpRequest);
                var result = await dataRefreshRequester.RequestDataRefresh(request, true);
                return new JsonResult(result);
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
        [Function(nameof(SubmitDataRefreshRequest))]
        public async Task SubmitDataRefreshRequest([TimerTrigger("%DataRefresh:CronTab%")] TimerInfo timerInfo)
        {
            var request = new DataRefreshRequest { ForceDataRefresh = false };
            await dataRefreshRequester.RequestDataRefresh(request, false);
        }

        /// <remarks>
        /// TEMPORARY (ATL-216): the request message is settled up front, before the job starts, rather than being auto-completed by the host
        /// when the refresh finishes a day or more later. Holding a broker lock for the lifetime of the job is statistically doomed - the SDK
        /// renews with a fixed 10-second margin and the first lock loss permanently retires renewal - and the redelivery that follows starts a
        /// second refresh on the same record, which corrupts both the run and the telemetry we are trying to measure. See
        /// ServiceBus_LockLoss_Findings.md. Settling early buys clean single-run metrics while the real fix (run lease + resume watchdog, or the
        /// Durable Functions migration) is being shaped.
        ///
        /// Accepted consequences while this is in place: there is no redelivery, so nothing resumes a refresh whose host dies, and the
        /// SqlException rethrow in DataRefreshOrchestrator.ContinueRefreshJob no longer resumes from its checkpoint - it just ends the run. The
        /// request message can also no longer dead-letter, so <see cref="RunDataRefreshCleanupAfterJobRequestDeadLetters"/> will not fire.
        /// Recovery in both cases is manual: call <see cref="RunDataRefreshCleanup"/>, then submit a new request.
        /// </remarks>
        [Function(nameof(RunDataRefresh))]
        public async Task RunDataRefresh(
            [ServiceBusTrigger(
                "%DataRefresh:RequestsTopic%",
                "%DataRefresh:RequestsTopicSubscription%",
                Connection = "MessagingServiceBus:ConnectionString",
                AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            var request = JsonConvert.DeserializeObject<ValidatedDataRefreshRequest>(message.Body.ToString());

            await messageActions.CompleteMessageAsync(message);
            logger.LogInformation(
                "Function {FunctionName}: request message completed up front; Record Id: {DataRefreshRecordId}; Delivery Count: {DeliveryCount}",
                nameof(RunDataRefresh),
                request.DataRefreshRecordId,
                message.DeliveryCount);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(request.DataRefreshRecordId);
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(RunDataRefreshCleanupAfterJobRequestDeadLetters))]
        public async Task RunDataRefreshCleanupAfterJobRequestDeadLetters(
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
        [Function(nameof(RunDataRefreshCleanup))]
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