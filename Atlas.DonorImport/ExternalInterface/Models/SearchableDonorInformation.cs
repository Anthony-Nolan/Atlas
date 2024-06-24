using System;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

// ReSharper disable InconsistentNaming

namespace Atlas.DonorImport.ExternalInterface.Models
{
    /// <summary>
    /// Info pertaining to donor search only.
    /// </summary>
    public class SearchableDonorInformation
    {
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public string ExternalDonorCode { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
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
    /// Update published after a donor change has been successfully applied.
    /// </summary>
    public class SearchableDonorUpdate
    {
        public DateTimeOffset PublishedDateTime { get; set; } = DateTimeOffset.UtcNow;
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public SearchableDonorInformation SearchableDonorInformation { get; set; }
    }
}