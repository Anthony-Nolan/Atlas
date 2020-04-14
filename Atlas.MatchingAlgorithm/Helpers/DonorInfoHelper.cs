using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Helpers
{
    public static class DonorInfoHelper
    {
        private static readonly IEnumerable<string> AdultDonorTypeValues = new List<string> { "adult", "a" };
        private static readonly IEnumerable<string> CordDonorTypeValues = new List<string> { "cord", "c" };

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
            input = input.ToLower();

            if (AdultDonorTypeValues.Contains(input))
            {
                return DonorType.Adult;
            }

            if (CordDonorTypeValues.Contains(input))
            {
                return DonorType.Cord;
            }

            throw new DonorInfoException($"Could not understand donor type {input}");
        }

        public static bool IsValidDonorType(string input)
        {
            return AdultDonorTypeValues.Concat(CordDonorTypeValues).Contains(input?.ToLower());
        }
    }
}