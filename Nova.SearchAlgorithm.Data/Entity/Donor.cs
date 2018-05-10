using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class Donor
    {
        [Key]
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }

        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }

        public DonorResult ToRawDonor()
        {
            return new DonorResult
            {
                DonorId = DonorId,
                DonorType = DonorType,
                RegistryCode = RegistryCode,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = A_1,
                    A_2 = A_2,
                    B_1 = B_1,
                    B_2 = B_2,
                    C_1 = C_1,
                    C_2 = C_2,
                    DQB1_1 = DQB1_1,
                    DQB1_2 = DQB1_2,
                    DRB1_1 = DRB1_1,
                    DRB1_2 = DRB1_2
                }
            };
        }
    }
}