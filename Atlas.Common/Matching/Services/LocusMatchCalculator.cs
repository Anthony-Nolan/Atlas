using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.Common.Matching.Services
{
    public interface ILocusMatchCalculator
    {
        /// <summary>
        /// Compares two different LocusInfo with  group of allele representations at each position for any possible matches.
        /// PGroups are the recommended typing to use here to guarantee a match.
        /// Other typings can be used, but the match count might not be correct if other typings are used, you should be certain you know what your doing if doing this.
        /// </summary>
        /// <returns>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is the potential for any allele to be present, and therefore the potential for a match.
        /// </returns>
        int MatchCount(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2);
    }

    public class LocusMatchCalculator : ILocusMatchCalculator
    {
        public int MatchCount(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            // Assume a match until we know otherwise - untyped loci should count as a potential match
            var matchCount = 2;

            if (alleleGroup1.Position1 == null ^ alleleGroup1.Position2 == null ||
                alleleGroup2.Position1 == null ^ alleleGroup2.Position2 == null)
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            if (alleleGroup1.Position1 != null &&
                alleleGroup1.Position2 != null &&
                alleleGroup2.Position1 != null &&
                alleleGroup2.Position2 != null)
            {
                // We have typed search and donor hla to compare
                matchCount = 0;

                var atLeastOneMatch =
                    alleleGroup1.Position1.Any(pg => alleleGroup2.Position1.Union(alleleGroup2.Position2).Contains(pg)) ||
                    alleleGroup1.Position2.Any(pg => alleleGroup2.Position1.Union(alleleGroup2.Position2).Contains(pg));

                if (atLeastOneMatch)
                {
                    matchCount++;
                }

                var twoMatches = DirectMatch(alleleGroup1, alleleGroup2) || CrossMatch(alleleGroup1, alleleGroup2);

                if (twoMatches)
                {
                    matchCount++;
                }
            }

            return matchCount;
        }

        private static bool DirectMatch(LocusInfo<IEnumerable<string>> alleleGroup1,
            LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            return alleleGroup1.Position1.Any(pg => alleleGroup2.Position1.Contains(pg)) &&
                   alleleGroup1.Position2.Any(pg => alleleGroup2.Position2.Contains(pg));
        }

        private static bool CrossMatch(LocusInfo<IEnumerable<string>> alleleGroup1,
            LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            return alleleGroup1.Position1.Any(pg => alleleGroup2.Position2.Contains(pg)) &&
                   alleleGroup1.Position2.Any(pg => alleleGroup2.Position1.Contains(pg));
        }
    }
}