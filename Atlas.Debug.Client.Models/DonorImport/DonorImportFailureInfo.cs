using System;
using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Information about why a donor update failed validation during donor import.
    /// </summary>
    public class DonorImportFailureInfo
    {
        /// <summary>
        /// Name of donor import file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Number of updates that failed to be applied.
        /// </summary>
        public int FailedUpdateCount { get; set; }

        /// <summary>
        /// Details of each failed update.
        /// </summary>
        public IEnumerable<FailedDonorUpdate> FailedUpdates { get; set; }
    }

    /// <summary>
    /// Info on a donor update failed to be imported into the donor store.
    /// </summary>
    public class FailedDonorUpdate
    {
        public string ExternalDonorCode { get; set; }
        public string DonorType { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }

        /// <summary>
        /// Name of property that failed validation.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Reason why <see cref="PropertyName"/> failed validation.
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// Date and time of the update failure.
        /// </summary>
        public DateTimeOffset FailureDateTime { get; set; }
    }
}
