using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Models.Extensions
{
    static class InputDonorExtensions
    {
        public static Donor ToDonorEntity(this InputDonorWithExpandedHla donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.MatchingHla?.A.Position1?.OriginalName,
                A_2 = donor.MatchingHla?.A.Position2?.OriginalName,
                B_1 = donor.MatchingHla?.B.Position1?.OriginalName,
                B_2 = donor.MatchingHla?.B.Position2?.OriginalName,
                C_1 = donor.MatchingHla?.C.Position1?.OriginalName,
                C_2 = donor.MatchingHla?.C.Position2?.OriginalName,
                DPB1_1 = donor.MatchingHla?.Dpb1.Position1?.OriginalName,
                DPB1_2 = donor.MatchingHla?.Dpb1.Position2?.OriginalName,
                DQB1_1 = donor.MatchingHla?.Dqb1.Position1?.OriginalName,
                DQB1_2 = donor.MatchingHla?.Dqb1.Position2?.OriginalName,
                DRB1_1 = donor.MatchingHla?.Drb1.Position1?.OriginalName,
                DRB1_2 = donor.MatchingHla?.Drb1.Position2?.OriginalName,
            };
        }

        public static Donor ToDonorEntity(this InputDonor donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.HlaNames?.A.Position1,
                A_2 = donor.HlaNames?.A.Position2,
                B_1 = donor.HlaNames?.B.Position1,
                B_2 = donor.HlaNames?.B.Position2,
                C_1 = donor.HlaNames?.C.Position1,
                C_2 = donor.HlaNames?.C.Position2,
                DPB1_1 = donor.HlaNames?.Dpb1.Position1,
                DPB1_2 = donor.HlaNames?.Dpb1.Position2,
                DQB1_1 = donor.HlaNames?.Dqb1.Position1,
                DQB1_2 = donor.HlaNames?.Dqb1.Position2,
                DRB1_1 = donor.HlaNames?.Drb1.Position1,
                DRB1_2 = donor.HlaNames?.Drb1.Position2,
            };
        }
    }
}
