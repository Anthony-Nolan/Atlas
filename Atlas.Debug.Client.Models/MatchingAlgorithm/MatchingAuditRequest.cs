using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.MatchingAlgorithm
{
    /// <summary>
    /// Request object for the matching algorithm audit debug endpoint.
    /// </summary>
    public class MatchingAuditRequest
    {
        /// <summary>
        /// External donor codes to look up in the matching algorithm's active transient database.
        /// </summary>
        public IReadOnlyCollection<string> ExternalDonorCodes { get; set; }
    }
}
