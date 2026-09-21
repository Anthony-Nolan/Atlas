using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.MatchingAlgorithm;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Functions.Functions.Debug
{
    /// <summary>
    /// Debug functions for auditing the matching algorithm state.
    /// </summary>
    public class MatchingAuditFunctions(
        IActiveRepositoryFactory activeRepositoryFactory,
        IActiveDatabaseProvider activeDatabaseProvider)
    {
        /// <summary>
        /// Returns a composite audit of the matching algorithm state: the active transient database
        /// and donor management log entries for the requested donors.
        /// </summary>
        [Function(nameof(GetMatchingAudit))]
        [ProducesResponseType(typeof(MatchingAuditResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMatchingAudit(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/audit/matching")]
            [RequestBodyType(typeof(MatchingAuditRequest), "Matching Audit Request")]
            HttpRequest request)
        {
            var auditRequest = await request.DeserialiseRequestBody<MatchingAuditRequest>();
            var externalDonorCodes = auditRequest.ExternalDonorCodes ?? [];

            var activeDb = activeDatabaseProvider.GetActiveDatabase();

            if (!externalDonorCodes.Any())
            {
                return new JsonResult(new MatchingAuditResult
                {
                    ActiveDatabase = activeDb.ToString(),
                    DonorManagementLogs = []
                });
            }

            var inspectionRepository = activeRepositoryFactory.GetDonorInspectionRepository();
            var donors = await inspectionRepository.GetAvailableDonorsByExternalDonorCodes(externalDonorCodes);
            var donorIds = donors.Select(d => d.DonorId).ToList();

            var managementLogRepository = activeRepositoryFactory.GetDonorManagementLogRepository();
            var managementLogs = donorIds.Count > 0
                ? await managementLogRepository.GetDonorManagementLogBatch(donorIds)
                : [];

            return new JsonResult(new MatchingAuditResult
            {
                ActiveDatabase = activeDb.ToString(),
                DonorManagementLogs = managementLogs.Select(log => new DonorManagementLogInfo
                {
                    DonorId = log.DonorId,
                    SequenceNumberOfLastUpdate = log.SequenceNumberOfLastUpdate,
                    LastUpdateDateTime = log.LastUpdateDateTime
                }).ToList()
            });
        }
    }
}
