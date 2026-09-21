using System;
using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.MatchingAlgorithm
{
    /// <summary>
    /// Composite audit result from the matching algorithm's active transient database.
    /// </summary>
    public class MatchingAuditResult
    {
        /// <summary>
        /// Name of the currently active transient database (DatabaseA or DatabaseB).
        /// </summary>
        public string ActiveDatabase { get; set; }

        /// <summary>
        /// Donor management log entries for the requested donors.
        /// </summary>
        public IReadOnlyCollection<DonorManagementLogInfo> DonorManagementLogs { get; set; }
    }

    /// <summary>
    /// Summary of a donor management log entry from the matching algorithm's transient database.
    /// </summary>
    public class DonorManagementLogInfo
    {
        /// <summary>
        /// Internal Atlas donor identifier.
        /// </summary>
        public int DonorId { get; set; }

        /// <summary>
        /// Sequence number of the last service bus message that updated this donor.
        /// </summary>
        public long SequenceNumberOfLastUpdate { get; set; }

        /// <summary>
        /// When the last update was applied.
        /// </summary>
        public DateTimeOffset LastUpdateDateTime { get; set; }
    }
}
