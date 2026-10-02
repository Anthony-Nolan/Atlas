using System;
using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Composite audit result from the donor import function app's SQL tables.
    /// </summary>
    public class DonorImportAuditResult
    {
        /// <summary>
        /// Import history record for the requested file name. Null when file name was not provided or not found.
        /// </summary>
        public DonorImportFileAudit FileAudit { get; set; }

        /// <summary>
        /// Last update times from the DonorLog table, keyed by external donor code.
        /// </summary>
        public IReadOnlyDictionary<string, DateTime> DonorLogLastUpdatedTimes { get; set; }

        /// <summary>
        /// Publishable donor update records for the requested donors.
        /// </summary>
        public IReadOnlyCollection<PublishableDonorUpdateInfo> PublishableDonorUpdates { get; set; }
    }

    /// <summary>
    /// Summary of a donor import history record.
    /// </summary>
    public class DonorImportFileAudit
    {
        /// <summary>
        /// Auto-generated record identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The donor import file name.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// When the file was uploaded to blob storage.
        /// </summary>
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// Current state of the file import (e.g. Started, Completed, FailedPermanent).
        /// </summary>
        public string FileState { get; set; }

        /// <summary>
        /// When import processing began.
        /// </summary>
        public DateTime ImportBegin { get; set; }

        /// <summary>
        /// When import processing completed. Null if still in progress.
        /// </summary>
        public DateTime? ImportEnd { get; set; }

        /// <summary>
        /// Number of donors successfully imported from this file.
        /// </summary>
        public int ImportedDonorsCount { get; set; }

        /// <summary>
        /// Number of donors that failed to import from this file.
        /// </summary>
        public int? FailedDonorCount { get; set; }
    }

    /// <summary>
    /// Summary of a publishable donor update record.
    /// </summary>
    public class PublishableDonorUpdateInfo
    {
        /// <summary>
        /// Auto-generated record identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Internal Atlas donor identifier.
        /// </summary>
        public int DonorId { get; set; }

        /// <summary>
        /// Whether the update has been published to the matching algorithm via service bus.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// When the update record was created.
        /// </summary>
        public DateTimeOffset CreatedOn { get; set; }

        /// <summary>
        /// When the update was published. Null if not yet published.
        /// </summary>
        public DateTimeOffset? PublishedOn { get; set; }
    }
}
