using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class Donor
    {
        public int Id { get; set; }
        public int DonorId { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }

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
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = A_1,
                    A_2 = A_2,
                    B_1 = B_1,
                    B_2 = B_2,
                    C_1 = C_1,
                    C_2 = C_2,
                    DPB1_1 = DPB1_1,
                    DPB1_2 = DPB1_2,
                    DQB1_1 = DQB1_1,
                    DQB1_2 = DQB1_2,
                    DRB1_1 = DRB1_1,
                    DRB1_2 = DRB1_2
                }
            };
        }

        public void CopyRawHlaFrom(InputDonor donor)
        {
            A_1 = donor.MatchingHla.A_1.OriginalName;
            A_2 = donor.MatchingHla.A_2.OriginalName;
            B_1 = donor.MatchingHla.B_1.OriginalName;
            B_2 = donor.MatchingHla.B_2.OriginalName;
            C_1 = donor.MatchingHla.C_1.OriginalName;
            C_2 = donor.MatchingHla.C_2.OriginalName;
            DRB1_1 = donor.MatchingHla.DRB1_1.OriginalName;
            DRB1_2 = donor.MatchingHla.DRB1_2.OriginalName;
            DQB1_1 = donor.MatchingHla.DQB1_1.OriginalName;
            DQB1_2 = donor.MatchingHla.DQB1_2.OriginalName;
        }
    }
}