using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Models.Extensions
{
    static class InputDonorExtensions
    {
        public static Donor ToDonorEntity(this InputDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.MatchingHla?.A_1?.Name,
                A_2 = donor.MatchingHla?.A_2?.Name,
                B_1 = donor.MatchingHla?.B_1?.Name,
                B_2 = donor.MatchingHla?.B_2?.Name,
                C_1 = donor.MatchingHla?.C_1?.Name,
                C_2 = donor.MatchingHla?.C_2?.Name,
                DRB1_1 = donor.MatchingHla?.DRB1_1?.Name,
                DRB1_2 = donor.MatchingHla?.DRB1_2?.Name,
                DQB1_1 = donor.MatchingHla?.DQB1_1?.Name,
                DQB1_2 = donor.MatchingHla?.DQB1_2?.Name
            };
        }

        public static Donor ToDonorEntity(this RawInputDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.HlaNames?.A_1,
                A_2 = donor.HlaNames?.A_2,
                B_1 = donor.HlaNames?.B_1,
                B_2 = donor.HlaNames?.B_2,
                C_1 = donor.HlaNames?.C_1,
                C_2 = donor.HlaNames?.C_2,
                DRB1_1 = donor.HlaNames?.DRB1_1,
                DRB1_2 = donor.HlaNames?.DRB1_2,
                DQB1_1 = donor.HlaNames?.DQB1_1,
                DQB1_2 = donor.HlaNames?.DQB1_2
            };
        }
    }
}
