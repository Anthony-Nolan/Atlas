using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.Common.Matching.Services
{
    public interface IAlleleGroupsMatchingCount
    {
        /// <summary>
        /// Compares two different LocusInfo with group of alleles at each position for any possible matches 
        /// </summary>
        /// <returns>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is a potential match.
        /// </returns>
        int MatchCount(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2);
    }

    public class AlleleGroupsMatchingCount : IAlleleGroupsMatchingCount
    {
        public int MatchCount(LocusInfo<IEnumerable<string>> alleleGroup1, LocusInfo<IEnumerable<string>> alleleGroup2)
        {
            // Assume a match until we know otherwise - untyped loci should count as a potential match
            var matchCount = 2;

            if (alleleGroup1.Position1 != null &&
                alleleGroup1.Position2 != null &&
                alleleGroup2.Position1 != null &&
                alleleGroup2.Position2 != null)
            {
                // We have typed search and donor hla to compare
                matchCount = 0;

                var atLeastOneMatch =
                    alleleGroup1.Position1.Any(pg =>
                        alleleGroup2.Position1.Union(alleleGroup2.Position2).Contains(pg)) ||
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