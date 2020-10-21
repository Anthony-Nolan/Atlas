using System;
using Atlas.Client.Models.Search;
using Atlas.DonorImport.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Config;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;

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

        public static DatabaseDonorType ToDatabaseType(this DonorType donorType)
        {
            return donorType switch
            {
                DonorType.Adult => DatabaseDonorType.Adult,
                DonorType.Cord => DatabaseDonorType.Cord,
                _ => throw new ArgumentOutOfRangeException(nameof(donorType), donorType, null)
            };
        }
    }
}