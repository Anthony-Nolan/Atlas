using System;
using Nova.DonorService.Client.Models;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Exceptions;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class DonorExtensions
    {
        public static RawInputDonor ToRawImportDonor(this Donor donor)
        {
            return new RawInputDonor
            {
                DonorId = donor.DonorId,
                RegistryCode = RegistryCodeFromString(donor.RegistryCode),
                DonorType = DonorTypeFromString(donor.DonorType),
                HlaNames = donor.HlaAsPhenotype()
            };
        }

        public static PhenotypeInfo<string> HlaAsPhenotype(this Donor donor)
        {
            return new PhenotypeInfo<string>
            {
                A_1 = donor.A_1,
                A_2 = donor.A_2,
                B_1 = donor.B_1,
                B_2 = donor.B_2,
                C_1 = donor.C_1,
                C_2 = donor.C_2,
                DPB1_1 = donor.DPB1_1,
                DPB1_2 = donor.DPB1_2,
                DQB1_1 = donor.DQB1_1,
                DQB1_2 = donor.DQB1_2,
                DRB1_1 = donor.DRB1_1,
                DRB1_2 = donor.DRB1_2
            };
        }

        public static RegistryCode RegistryCodeFromString(string input)
        {
            if (Enum.TryParse(input, out RegistryCode code))
            {
                return code;
            }
            throw new DonorImportException($"Could not understand registry code {input}");
        }

        public static DonorType DonorTypeFromString(string input)
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