using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Extensions
{
    public static class InputDonorWithExpandedHlaExtensions
    {
        public static InputDonor ToInputDonor(this InputDonorWithExpandedHla donor)
        {
            return new InputDonor
            {
                DonorId = donor.DonorId,
                RegistryCode = donor.RegistryCode,
                DonorType = donor.DonorType,
                HlaNames = donor.MatchingHla.Map((l, p, expandedHla) => expandedHla?.OriginalName)
            };
        }

        public static Donor ToDonor(this InputDonorWithExpandedHla donor)
        {
            return new Donor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                A_1 = donor.MatchingHla.A.Position1.OriginalName,
                A_2 = donor.MatchingHla.A.Position2.OriginalName,
                B_1 = donor.MatchingHla.B.Position1.OriginalName,
                B_2 = donor.MatchingHla.B.Position2.OriginalName,
                C_1 = donor.MatchingHla.C.Position1?.OriginalName,
                C_2 = donor.MatchingHla.C.Position2?.OriginalName,
                DRB1_1 = donor.MatchingHla.Drb1.Position1.OriginalName,
                DRB1_2 = donor.MatchingHla.Drb1.Position2.OriginalName,
                DQB1_1 = donor.MatchingHla.Dqb1.Position1?.OriginalName,
                DQB1_2 = donor.MatchingHla.Dqb1.Position2?.OriginalName,
                DPB1_1 = donor.MatchingHla.Dpb1.Position1?.OriginalName,
                DPB1_2 = donor.MatchingHla.Dpb1.Position2?.OriginalName,
            };
        }
    }
}
