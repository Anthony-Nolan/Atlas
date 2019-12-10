using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Exceptions;
using System;

namespace Nova.SearchAlgorithm.Helpers
{
    public static class DonorInfoHelper
    {
        public static RegistryCode RegistryCodeFromString(string input)
        {
            if (Enum.TryParse(input, out RegistryCode code))
            {
                return code;
            }

            throw new DonorInfoException($"Could not understand registry code {input}");
        }

        public static DonorType DonorTypeFromString(string input)
        {
            if (Enum.TryParse(input, out DonorType code))
            {
                return code;
            }

            throw new DonorInfoException($"Could not understand donor type {input}");
        }
    }
}