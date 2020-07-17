using System;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.Common.Matching.Services
{
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
        int MatchCount(LocusInfo<string> patientHla, LocusInfo<string> donorHla);
    }

    internal class StringBasedLocusMatchCalculator : IStringBasedLocusMatchCalculator
    {
        public int MatchCount(LocusInfo<string> patientHla, LocusInfo<string> donorHla)
        {
            if (patientHla.SinglePositionNull() || donorHla.SinglePositionNull())
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            // Untyped loci are considered to have a "potential" match count of 2 - as they are not guaranteed not to match.
            if (IsUntyped(patientHla) || IsUntyped(donorHla))
            {
                return 2;
            }
            
            if (TwoOutOfTwoStringMatch(patientHla, donorHla))
            {
                return 2;
            }

            // relies on 2/2 being called first
            return AtLeastOneStringMatch(patientHla, donorHla) ? 1 : 0;
        }

        private static bool TwoOutOfTwoStringMatch(LocusInfo<string> locus1, LocusInfo<string> locus2)
        {
            if (locus1 == null || locus2 == null)
            {
                return false;
            }

            var directMatch = ExpressingHlaMatch(locus1.Position1, locus2.Position1) && ExpressingHlaMatch(locus1.Position2, locus2.Position2);
            var crossMatch = ExpressingHlaMatch(locus1.Position1, locus2.Position2) && ExpressingHlaMatch(locus1.Position2, locus2.Position1);
            return directMatch || crossMatch;
        }

        private static bool AtLeastOneStringMatch(LocusInfo<string> locus1, LocusInfo<string> locus2)
        {
            return ExpressingHlaMatch(locus1.Position1, locus2.Position1) ||
                   ExpressingHlaMatch(locus1.Position1, locus2.Position2) ||
                   ExpressingHlaMatch(locus1.Position2, locus2.Position1) ||
                   ExpressingHlaMatch(locus1.Position2, locus2.Position2);
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