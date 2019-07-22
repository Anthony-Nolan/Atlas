using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Helpers;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class DonorInfoForSearchAlgorithmExtensions
    {
        public static InputDonor ToInputDonor(this DonorInfoForSearchAlgorithm donor)
        {
            return new InputDonor
            {
                DonorId = donor.DonorId,
                RegistryCode = DonorInfoHelper.RegistryCodeFromString(donor.RegistryCode),
                DonorType = DonorInfoHelper.DonorTypeFromString(donor.DonorType),
                HlaNames = donor.HlaAsPhenotype()
            };
        }

        private static PhenotypeInfo<string> HlaAsPhenotype(this DonorInfoForSearchAlgorithm donor)
        {
            return new PhenotypeInfo<string>
            {
                A = { Position1 = donor.A_1, Position2 = donor.A_2 },
                B = { Position1 = donor.B_1, Position2 = donor.B_2 },
                C = { Position1 = donor.C_1, Position2 = donor.C_2 },
                Dpb1 = { Position1 = donor.DPB1_1, Position2 = donor.DPB1_2 },
                Dqb1 = { Position1 = donor.DQB1_1, Position2 = donor.DQB1_2 },
                Drb1 = { Position1 = donor.DRB1_1, Position2 = donor.DRB1_2 },
            };
        }
    }
}