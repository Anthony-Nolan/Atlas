using Atlas.Common.GeneticData.Hla.Models;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.Config
{
    internal static class VerificationSettings
    {
        /// <summary>
        /// HLA typing categories expected within the haplotype frequency set used to generate the test harness.
        /// These will also be the categories of typings that have not been masked.
        /// </summary>
        internal static ISet<HlaTypingCategory> HaplotypeFrequencySetCategories => new[]
        {
            HlaTypingCategory.Allele,
            HlaTypingCategory.GGroup
        }.ToHashSet();
    }

    internal static class CategoryExtensions
    {
        public static bool IsNotHaplotypeFrequencySetCategory(this HlaTypingCategory category)
        {
            return !VerificationSettings.HaplotypeFrequencySetCategories.Contains(category);
        }
    }
}
