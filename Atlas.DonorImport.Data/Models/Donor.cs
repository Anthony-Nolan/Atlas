using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable InconsistentNaming

namespace Atlas.DonorImport.Data.Models
{
    [Table("Donors")]
    public class Donor
    {
        public int Id { get; set; }

        // TODO: ATLAS-167: ADD unique index to DonorId
        public int DonorId { get; set; }
        
        public DatabaseDonorType DonorType { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
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
        public string Hash { get; set; }
    }
}