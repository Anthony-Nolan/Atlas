using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Request object for the donor import audit debug endpoint.
    /// </summary>
    public class DonorImportAuditRequest
    {
        /// <summary>
        /// External donor codes to look up across donor import tables (DonorLog, PublishableDonorUpdates).
        /// </summary>
        public IReadOnlyCollection<string> ExternalDonorCodes { get; set; }

        /// <summary>
        /// Optional donor import file name to look up in the import history table.
        /// </summary>
        public string FileName { get; set; }
    }
}
