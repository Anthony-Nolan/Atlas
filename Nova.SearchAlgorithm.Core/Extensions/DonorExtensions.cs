using System;
using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Exceptions;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class DonorExtensions
    {
        public static InputDonor ToInputDonor(this DonorInfoForSearchAlgorithm donor)
        {
            return new InputDonor
            {
                DonorId = donor.DonorId,
                RegistryCode = RegistryCodeFromString(donor.RegistryCode),
                DonorType = DonorTypeFromString(donor.DonorType),
                HlaNames = donor.HlaAsPhenotype()
            };
        }

        private static PhenotypeInfo<string> HlaAsPhenotype(this DonorInfoForSearchAlgorithm donor)
        {
            return new PhenotypeInfo<string>
            {
                A = { Position1 = donor.A_1, Position2 = donor.A_2},
                B = { Position1 = donor.B_1, Position2 = donor.B_2},
                C = { Position1 = donor.C_1, Position2 = donor.C_2},
                Dpb1 = { Position1 = donor.DPB1_1, Position2 = donor.DPB1_2},
                Dqb1 = { Position1 = donor.DQB1_1, Position2 = donor.DQB1_2},
                Drb1 = { Position1 = donor.DRB1_1, Position2 = donor.DRB1_2},
            };
        }

        private static RegistryCode RegistryCodeFromString(string input)
        {
            if (Enum.TryParse(input, out RegistryCode code))
            {
                return code;
            }
            throw new DonorImportException($"Could not understand registry code {input}");
        }

        private static DonorType DonorTypeFromString(string input)
        {
            switch (input.ToLower())
            {
                case "adult":
                case "a":
                    return DonorType.Adult;
                case "cord":
                case "c":
                    return DonorType.Cord;
                default:
                    throw new DonorImportException($"Could not understand donor type {input}");
            }
        }
    }
}