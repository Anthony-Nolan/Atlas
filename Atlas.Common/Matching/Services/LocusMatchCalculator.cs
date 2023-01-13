using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.Common.Matching.Services
{
    public interface ILocusMatchCalculator
    {
        /// <summary>
        /// Compares two different LocusInfo with a group of allele representations at each position for any possible matches.
        /// PGroups are the recommended typing to use here to guarantee a match.
        /// Other typings can be used, but the match count might not be correct if other typings are used, you should be certain you know what your doing if doing this.
        /// </summary>
        /// <returns>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is the potential for any allele to be present, and therefore the potential for a match.
        /// </returns>
        int MatchCount(LocusInfo<IEnumerable<string>> patientHla, LocusInfo<IEnumerable<string>> donorHla);
    }

    public class LocusMatchCalculator : ILocusMatchCalculator
    {
        public int MatchCount(LocusInfo<IEnumerable<string>> patientHla, LocusInfo<IEnumerable<string>> donorHla)
        {
            // Assume a match until we know otherwise - untyped loci should count as a potential match
            var matchCount = 2;

            if (patientHla.SinglePositionNull() || donorHla.SinglePositionNull())
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            if (patientHla.Position1And2NotNull() && donorHla.Position1And2NotNull())
            {
                // We have typed search and donor hla to compare
                matchCount = 0;

                var atLeastOneMatch =
                    patientHla.Position1.Any(pg => donorHla.Position1.Union(donorHla.Position2).Contains(pg)) ||
                    patientHla.Position2.Any(pg => donorHla.Position1.Union(donorHla.Position2).Contains(pg));

                if (atLeastOneMatch)
                {
                    matchCount++;
                }

                var twoMatches = DirectMatch(patientHla, donorHla) || CrossMatch(patientHla, donorHla);

                if (twoMatches)
                {
                    matchCount++;
                }
            }

            return matchCount;
        }

        private static bool DirectMatch(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            return alleleGroup1.Position1.Any(pg => alleleGroup2.Position1.Contains(pg)) &&
                   alleleGroup1.Position2.Any(pg => alleleGroup2.Position2.Contains(pg));
        }

        private static bool CrossMatch(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            return alleleGroup1.Position1.Any(pg => alleleGroup2.Position2.Contains(pg)) &&
                   alleleGroup1.Position2.Any(pg => alleleGroup2.Position1.Contains(pg));
        }
    }
}