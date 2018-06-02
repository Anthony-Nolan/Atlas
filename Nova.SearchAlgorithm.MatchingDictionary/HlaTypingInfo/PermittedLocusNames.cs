using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo
{
    /// <summary>
    /// The matching dictionary will only contain typing data for a subset of all the possible HLA loci.
    /// This class defines the names of these permitted loci, and their typing method variants.
    /// </summary>
    internal static class PermittedLocusNames
    {
        private class LocusName
        {
            public string Serology { get; }
            public string Molecular { get; }
            public MatchLocus Match { get; }

            public LocusName(string serology, string molecular, MatchLocus match)
            {
                Serology = serology;
                Molecular = molecular;
                Match = match;
            }
        }

        private static readonly List<LocusName> NamesOfPermittedLoci = new List<LocusName>
        {
            new LocusName("A", "A*", MatchLocus.A),
            new LocusName("B", "B*", MatchLocus.B),
            new LocusName("Cw", "C*", MatchLocus.C),
            new LocusName("DQ", "DQB1*", MatchLocus.Dqb1),
            new LocusName("DR", "DRB1*", MatchLocus.Drb1)
        };

        public static bool IsPermittedMolecularLocus(string locusName)
        {
            return NamesOfPermittedLoci.Select(n => n.Molecular).Contains(locusName);
        }

        public static bool IsPermittedSerologyLocus(string locusName)
        {
            return NamesOfPermittedLoci.Select(n => n.Serology).Contains(locusName);
        }

        public static IEnumerable<MatchLocus> GetPermittedMatchLoci()
        {
            return NamesOfPermittedLoci.Select(n => n.Match);
        }

        public static MatchLocus GetMatchLocusNameFromTypingLocusIfExists(TypingMethod typingMethod, string locusName)
        {
            var permittedLocus = NamesOfPermittedLoci.FirstOrDefault(locus =>
                typingMethod == TypingMethod.Molecular ?
                    locus.Molecular.Equals(locusName) :
                    locus.Serology.Equals(locusName));

            if (permittedLocus == null)
            {
                throw new PermittedLocusException(locusName);
            }

            return permittedLocus.Match;
        }

        public static string GetSerologyLocusNameFromMolecularIfExists(string molecularLocusName)
        {
            var permittedLocus = NamesOfPermittedLoci.First(l => l.Molecular.Equals(molecularLocusName));

            if (permittedLocus == null)
            {
                throw new PermittedLocusException(molecularLocusName);
            }
                
            return permittedLocus.Serology;
        }

        public static string GetMolecularLocusNameFromMatchIfExists(MatchLocus matchLocusName)
        {
            var permittedLocus = NamesOfPermittedLoci.First(l => l.Match.Equals(matchLocusName));

            if (permittedLocus == null)
            {
                throw new PermittedLocusException(matchLocusName.ToString());
            }

            return permittedLocus.Molecular;
        }
    }
}
