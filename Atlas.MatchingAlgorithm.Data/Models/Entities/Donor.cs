using System.ComponentModel.DataAnnotations;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    public class Donor
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }

        [Required]
        public bool IsAvailableForSearch { get; set; }

        [Required]
        public string A_1 { get; set; }

        [Required]
        public string A_2 { get; set; }

        [Required]
        public string B_1 { get; set; }

        [Required]
        public string B_2 { get; set; }

        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DPB1_1 { get; set; }
        public string DPB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }

        [Required]
        public string DRB1_1 { get; set; }

        [Required]
        public string DRB1_2 { get; set; }

        public DonorInfo.DonorInfo ToDonorInfo()
        {
            return new DonorInfo.DonorInfo
            {
                DonorId = DonorId,
                DonorType = DonorType,
                IsAvailableForSearch = IsAvailableForSearch,
                HlaNames = new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(A_1, A_2),
                    B = new LocusInfo<string>(B_1, B_2),
                    C = new LocusInfo<string>(C_1, C_2),
                    Dpb1 = new LocusInfo<string>(DPB1_1, DPB1_2),
                    Dqb1 = new LocusInfo<string>(DQB1_1, DQB1_2),
                    Drb1 = new LocusInfo<string>(DRB1_1, DRB1_2),
                }
            };
        }
    }
}