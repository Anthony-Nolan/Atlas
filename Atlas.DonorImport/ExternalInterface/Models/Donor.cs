using Atlas.MatchingAlgorithm.Client.Models.Donors;
using System;

// ReSharper disable InconsistentNaming

namespace Atlas.DonorImport.ExternalInterface.Models
{
    public class Donor
    {
        public int AtlasDonorId { get; set; }

        public string ExternalDonorCode { get; set; }

        public DonorType DonorType { get; set; }

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
}