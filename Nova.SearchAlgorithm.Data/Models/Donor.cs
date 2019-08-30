using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class Donor
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }

        [Required]
        public bool IsAvailableForSearch { get; set; }

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

        public DonorResult ToDonorResult()
        {
            return new DonorResult
            {
                DonorId = DonorId,
                DonorType = DonorType,
                RegistryCode = RegistryCode,
                IsAvailableForSearch = IsAvailableForSearch,
                HlaNames = new PhenotypeInfo<string>
                {
                    A = {Position1 = A_1, Position2 = A_2},
                    B = {Position1 = B_1, Position2 = B_2},
                    C = {Position1 = C_1, Position2 = C_2},
                    Dpb1 = {Position1 = DPB1_1, Position2 = DPB1_2},
                    Dqb1 = {Position1 = DQB1_1, Position2 = DQB1_2},
                    Drb1 = {Position1 = DRB1_1, Position2 = DRB1_2},
                }
            };
        }
    }
}