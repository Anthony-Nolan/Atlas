using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    internal static class DonorExtensions
    {
        internal static DonorTableEntity ToTableEntity(this RawInputDonor donor)
        {
            return new DonorTableEntity(donor.RegistryCode.ToString(), donor.DonorId.ToString())
            {
                DonorId = donor.DonorId,
                RegistryCode = (int)donor.RegistryCode,
                DonorType = (int)donor.DonorType,
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

        internal static DonorResult ToRawDonor(this DonorTableEntity result)
        {
            var donorResult = new DonorResult
            {
                DonorId = result.DonorId,
                RegistryCode = (RegistryCode)result.RegistryCode,
                DonorType = (DonorType)result.DonorType,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = result.A_1,
                    A_2 = result.A_2,
                    B_1 = result.B_1,
                    B_2 = result.B_2,
                    C_1 = result.C_1,
                    C_2 = result.C_2,
                    DRB1_1 = result.DRB1_1,
                    DRB1_2 = result.DRB1_2,
                    DQB1_1 = result.DQB1_1,
                    DQB1_2 = result.DQB1_2
                }
            };
            return donorResult;
        }
    }
}