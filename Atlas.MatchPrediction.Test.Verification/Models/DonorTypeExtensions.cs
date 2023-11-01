using Atlas.Client.Models.Search;
using Atlas.MatchPrediction.Test.Verification.Config;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using System;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal static class DonorTypeExtensions
    {
        public static DonorType ToDonorType(this SimulatedHlaTypingCategory category)
        {
            return category switch
            {
                SimulatedHlaTypingCategory.Genotype => VerificationConstants.GenotypeSearchDonorType,
                SimulatedHlaTypingCategory.Masked => VerificationConstants.MaskedSearchDonorType,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }
    }
}