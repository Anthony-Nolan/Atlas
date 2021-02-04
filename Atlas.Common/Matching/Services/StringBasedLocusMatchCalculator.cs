using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.Common.Matching.Services
{
    public enum UntypedLocusBehaviour
    {
        TreatAsMatch,
        TreatAsMismatch,
        Throw
    }

    public interface IStringBasedLocusMatchCalculator
    {
        /// <summary>
        /// Calculates match count based *solely* on string comparison of the provided hla data.
        ///
        /// Assumes that null alleles have already been handled by copying the expressing allele to the null position - nulls here will be
        /// treated as missing loci, not null-expressing alleles 
        ///
        /// This will *NOT* give accurate results for any resolution other than P-Group: as P-Group matching is the minimum requirement to
        /// consider a pair of Loci a match, any other resolution can (and likely will) correspond to multiple P-Groups: for which we should use
        /// the full matching logic in <see cref="ILocusMatchCalculator"/>
        ///
        /// This method is significantly faster than that in <see cref="ILocusMatchCalculator"/>, but can only be used in very limited cases. 
        /// </summary>
        /// <returns></returns>
        int MatchCount(
            LocusInfo<string> patientHla,
            LocusInfo<string> donorHla,
            UntypedLocusBehaviour untypedLocusBehaviour = UntypedLocusBehaviour.TreatAsMatch);

        // Just lookup, no count here
        int IndexBasedMatchCount(
            int? patientHla,
            int? donorHla,
            (int[] flatMatchCounts, int nestedArrayCount) matchCounts,
            UntypedLocusBehaviour untypedLocusBehaviour = UntypedLocusBehaviour.TreatAsMatch);
    }

    internal class StringBasedLocusMatchCalculator : IStringBasedLocusMatchCalculator
    {
        public int MatchCount(LocusInfo<string> patientHla, LocusInfo<string> donorHla, UntypedLocusBehaviour untypedLocusBehaviour)
        {
            // ReSharper disable InconsistentNaming
            // deconstruct once to avoid additional property access
            var p_1 = patientHla.Position1;
            var p_2 = patientHla.Position2;
            var d_1 = donorHla.Position1;
            var d_2 = donorHla.Position2;
            
            if (p_1 == null ^ p_2 == null || d_1 == null ^ d_2 == null)
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            // have already confirmed that either both are null, or neither is - so just checking one position is enough to identify a missing locus here 
            if (d_1 == null || p_1 == null)
            {
                return untypedLocusBehaviour switch
                {
                    // Untyped loci are considered to have a "potential" match count of 2 - as they are not guaranteed not to match.
                    UntypedLocusBehaviour.TreatAsMatch => 2,
                    UntypedLocusBehaviour.TreatAsMismatch => 0,
                    UntypedLocusBehaviour.Throw => throw new ArgumentException(
                        $"Locus is untyped, and {nameof(UntypedLocusBehaviour)} is set to {UntypedLocusBehaviour.Throw}"),
                    _ => throw new ArgumentOutOfRangeException(nameof(untypedLocusBehaviour))
                };
            }

            var match_1_1 = ExpressingHlaMatch(p_1, d_1);
            var match_1_2 = ExpressingHlaMatch(p_1, d_2);
            var match_2_1 = ExpressingHlaMatch(p_2, d_1);
            var match_2_2 = ExpressingHlaMatch(p_2, d_2);
            // ReSharper restore InconsistentNaming

            var directMatch = match_1_1 && match_2_2;
            var crossMatch = match_1_2 && match_2_1;
            var twoOutOfTwo = directMatch || crossMatch;

            if (twoOutOfTwo)
            {
                return 2;
            }

            // relies on 2/2 being calculated first
            return match_1_1 || match_1_2 || match_2_1 || match_2_2 ? 1 : 0;
        }

        /// <inheritdoc />
        public int IndexBasedMatchCount(
            int? patientHla,
            int? donorHla,
            (int[] flatMatchCounts, int nestedArrayCount) matchCounts,
            UntypedLocusBehaviour untypedLocusBehaviour = UntypedLocusBehaviour.TreatAsMatch)
        {
            if (patientHla == null || donorHla == null)
            {
                return untypedLocusBehaviour switch
                {
                    // Untyped loci are considered to have a "potential" match count of 2 - as they are not guaranteed not to match.
                    UntypedLocusBehaviour.TreatAsMatch => 2,
                    UntypedLocusBehaviour.TreatAsMismatch => 0,
                    UntypedLocusBehaviour.Throw => throw new ArgumentException(
                        $"Locus is untyped, and {nameof(UntypedLocusBehaviour)} is set to {UntypedLocusBehaviour.Throw}"),
                    _ => throw new ArgumentOutOfRangeException(nameof(untypedLocusBehaviour))
                };
            }

            var combinedIndex = patientHla.Value * matchCounts.nestedArrayCount + donorHla.Value;
            return matchCounts.flatMatchCounts[combinedIndex];

            // unsafe
            // {
            //     fixed (int* pntr = matchCounts.flatMatchCounts)
            //     {
            //         
            //         var combinedIndex = patientHla.Value * matchCounts.nestedArrayCount + donorHla.Value;
            //         return pntr[combinedIndex];
            //     }
            // }
        }

        private static bool ExpressingHlaMatch(string locus1, string locus2)
        {
            // string.CompareOrdinal is faster than == 
            return string.CompareOrdinal(locus1, locus2) == 0;
        }
    }
}