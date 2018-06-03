using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Services
{
    static class DonorExtensions
    {
        public static RawInputDonor ToRawImportDonor(this Donor donor)
        {
            return new RawInputDonor
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType,
                RegistryCode = donor.RegistryCode,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = donor.A_1,
                    A_2 = donor.A_2,
                    B_1 = donor.B_1,
                    B_2 = donor.B_2,
                    C_1 = donor.C_1,
                    C_2 = donor.C_2,
                    DQB1_1 = donor.DQB1_1,
                    DQB1_2 = donor.DQB1_2,
                    DRB1_1 = donor.DRB1_1,
                    DRB1_2 = donor.DRB1_2
                }
            };
        }
    }
}