using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class Donor
    {
        [Key]
        public int DonorId { get; set; }

        // TODO:NOVA-929 make donor types a strongly typed Enum
        public string DonorType { get; set; }
        // TODO:NOVA-931 this might need to be a string?
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

        public SearchableDonor ToSearchableDonor()
        {
            return new SearchableDonor
            {
                DonorId = DonorId,
                DonorType = DonorType,
                RegistryCode = RegistryCode
            };
        }

        public RawDonor ToRawDonor()
        {
            return new RawDonor
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