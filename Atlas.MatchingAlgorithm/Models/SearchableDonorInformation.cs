using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Models
{
    /// <summary>
    /// Donor info containing only the information required to search for donors - e.g. by the search algorithm service
    /// This only includes the bare minimum for a search for performance reasons.
    /// </summary>
    public class SearchableDonorInformation
    {
        public int DonorId { get; set; }
        public string DonorType { get; set; }
        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DPB1_1 { get; set; }
        public string DPB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
    }

    /// <summary>
    /// A page of donor info containing only the information required by the search-algorithm.
    /// </summary>
    public class SearchableDonorInformationPage
    {
        //TODO: ATLAS-186 If we use this class at all, then Clean it class up.
        // "ResultsPerPage" => "RequestedResultsPerPage".
        // Add "NextId" (nullable), and use that to indicate whether more should be fetched.
        // ??Drop LastId??
        public int ResultsPerPage { get; set; }
        public int? LastId { get; set; }
        public IEnumerable<SearchableDonorInformation> DonorsInfo { get; set; }
    }

    public class SearchableDonorUpdate
    {
        /// <summary>
        /// The id of the audit table row that triggered this donor update
        /// </summary>
        [JsonIgnore]
        public int AuditId { get; set; }

        public string DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public SearchableDonorInformation SearchableDonorInformation { get; set; }
        public DateTimeOffset? PublishedDateTime { get; set; }
    }
}