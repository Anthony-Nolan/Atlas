using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.Data.Repositories;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Functions.Functions.Debug
{
    /// <summary>
    /// Debug functions for auditing the donor import pipeline state.
    /// </summary>
    public class DonorImportAuditFunctions(
        IDonorImportHistoryRepository historyRepository,
        IDonorImportLogRepository logRepository,
        IPublishableDonorUpdatesRepository publishableUpdatesRepository,
        IDonorReadRepository donorReadRepository)
    {
        /// <summary>
        /// Returns a composite audit of the donor import pipeline: import history for the file,
        /// donor log update times, and publishable donor update records for the requested donors.
        /// </summary>
        [Function(nameof(GetDonorImportAudit))]
        [ProducesResponseType(typeof(DonorImportAuditResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDonorImportAudit(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/audit/donorImport")]
            [RequestBodyType(typeof(DonorImportAuditRequest), "Donor Import Audit Request")]
            HttpRequest request)
        {
            var auditRequest = await request.DeserialiseRequestBody<DonorImportAuditRequest>();

            DonorImportFileAudit fileAudit = null;
            if (!string.IsNullOrEmpty(auditRequest.FileName))
            {
                var records = await historyRepository.GetByFileName(auditRequest.FileName);
                var record = records.FirstOrDefault();
                if (record != null)
                {
                    fileAudit = new DonorImportFileAudit
                    {
                        Id = record.Id,
                        Filename = record.Filename,
                        UploadTime = record.UploadTime,
                        FileState = record.FileState.ToString(),
                        ImportBegin = record.ImportBegin,
                        ImportEnd = record.ImportEnd,
                        ImportedDonorsCount = record.ImportedDonorsCount,
                        FailedDonorCount = record.FailedDonorCount
                    };
                }
            }

            var donorCodes = auditRequest.ExternalDonorCodes?.ToList() ?? [];

            IReadOnlyDictionary<string, DateTime> donorLogTimes = new Dictionary<string, DateTime>();
            List<PublishableDonorUpdateInfo> publishableUpdates = [];

            if (donorCodes.Count > 0)
            {
                donorLogTimes = await logRepository.GetLastUpdatedTimes(donorCodes);

                var donorIdMap = await donorReadRepository.GetDonorIdsByExternalDonorCodes(donorCodes);
                var donorIds = donorIdMap.Values.ToList();

                if (donorIds.Count > 0)
                {
                    publishableUpdates = (await publishableUpdatesRepository.GetByDonorIds(donorIds))
                        .Select(u => new PublishableDonorUpdateInfo
                        {
                            Id = u.Id,
                            DonorId = u.DonorId,
                            IsPublished = u.IsPublished,
                            CreatedOn = u.CreatedOn,
                            PublishedOn = u.PublishedOn
                        })
                        .ToList();
                }
            }

            return new JsonResult(new DonorImportAuditResult
            {
                FileAudit = fileAudit,
                DonorLogLastUpdatedTimes = donorLogTimes,
                PublishableDonorUpdates = publishableUpdates
            });
        }
    }
}
