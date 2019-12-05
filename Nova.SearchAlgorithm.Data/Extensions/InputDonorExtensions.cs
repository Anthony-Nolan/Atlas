using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Data.Models.Entities;

namespace Nova.SearchAlgorithm.Data.Extensions
{
    public static class InputDonorExtensions
    {
        public static Donor ToDonor(this InputDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.HlaNames.A.Position1,
                A_2 = donor.HlaNames.A.Position2,
                B_1 = donor.HlaNames.B.Position1,
                B_2 = donor.HlaNames.B.Position2,
                C_1 = donor.HlaNames.C.Position1,
                C_2 = donor.HlaNames.C.Position2,
                DRB1_1 = donor.HlaNames.Drb1.Position1,
                DRB1_2 = donor.HlaNames.Drb1.Position2,
                DQB1_1 = donor.HlaNames.Dqb1.Position1,
                DQB1_2 = donor.HlaNames.Dqb1.Position2,
                DPB1_1 = donor.HlaNames.Dpb1.Position1,
                DPB1_2 = donor.HlaNames.Dpb1.Position2,
            };
        }
    }
}
