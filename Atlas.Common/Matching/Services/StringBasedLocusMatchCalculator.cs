using System;
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
    }

    internal class StringBasedLocusMatchCalculator : IStringBasedLocusMatchCalculator
    {
        public int MatchCount(LocusInfo<string> patientHla, LocusInfo<string> donorHla, UntypedLocusBehaviour untypedLocusBehaviour)
        {
            if (patientHla.SinglePositionNull() || donorHla.SinglePositionNull())
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            if (IsUntyped(patientHla) || IsUntyped(donorHla))
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

            // ReSharper disable InconsistentNaming
            var match_1_1 = ExpressingHlaMatch(patientHla.Position1, donorHla.Position1);
            var match_1_2 = ExpressingHlaMatch(patientHla.Position1, donorHla.Position2);
            var match_2_1 = ExpressingHlaMatch(patientHla.Position2, donorHla.Position1);
            var match_2_2 = ExpressingHlaMatch(patientHla.Position2, donorHla.Position2);
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

        // nulls = non expressing alleles (aka "null alleles") for pGroups.
        private static bool ExpressingHlaMatch(string locus1, string locus2)
        {
            if (locus1 == null || locus2 == null)
            {
                return false;
            }

            return locus1 == locus2;
        }

        private static bool IsUntyped(LocusInfo<string> hla)
        {
            return hla.Position1And2Null();
        }
    }
}