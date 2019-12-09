using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Data.Models.Entities;

namespace Nova.SearchAlgorithm.Data.Extensions
{
    public static class DonorInfoExtensions
    {
        public static Donor ToDonor(this DonorInfo donorInfo)
        {
            return new Donor
            {
                DonorId = donorInfo.DonorId,
                DonorType = donorInfo.DonorType,
                RegistryCode = donorInfo.RegistryCode,
                A_1 = donorInfo.HlaNames.A.Position1,
                A_2 = donorInfo.HlaNames.A.Position2,
                B_1 = donorInfo.HlaNames.B.Position1,
                B_2 = donorInfo.HlaNames.B.Position2,
                C_1 = donorInfo.HlaNames.C.Position1,
                C_2 = donorInfo.HlaNames.C.Position2,
                DRB1_1 = donorInfo.HlaNames.Drb1.Position1,
                DRB1_2 = donorInfo.HlaNames.Drb1.Position2,
                DQB1_1 = donorInfo.HlaNames.Dqb1.Position1,
                DQB1_2 = donorInfo.HlaNames.Dqb1.Position2,
                DPB1_1 = donorInfo.HlaNames.Dpb1.Position1,
                DPB1_2 = donorInfo.HlaNames.Dpb1.Position2,
            };
        }
    }
}
