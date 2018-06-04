using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo
{
    /// <summary>
    /// The matching dictionary will only contain typing data for a subset of all the possible HLA loci.
    /// This class defines the names of these permitted loci, and their variants according to typing method.
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
        
        public static IEnumerable<string> SerologyLoci => NamesOfPermittedLoci.Select(n => n.Serology);
        public static IEnumerable<string> MolecularLoci => NamesOfPermittedLoci.Select(n => n.Molecular);
        public static IEnumerable<MatchLocus> MatchLoci => NamesOfPermittedLoci.Select(n => n.Match);

        private static readonly List<LocusName> NamesOfPermittedLoci = new List<LocusName>
        {
            new LocusName("A", "A*", MatchLocus.A),
            new LocusName("B", "B*", MatchLocus.B),
            new LocusName("Cw", "C*", MatchLocus.C),
            new LocusName("DQ", "DQB1*", MatchLocus.Dqb1),
            new LocusName("DR", "DRB1*", MatchLocus.Drb1)
        };

        public static MatchLocus GetMatchLocusFromWmdaLocus(string wmdaLocus)
        {
            var locus = NamesOfPermittedLoci.FirstOrDefault(
                    l => l.Molecular.Equals(wmdaLocus) || l.Serology.Equals(wmdaLocus));

            if (locus == null)
                throw new ArgumentException($"{wmdaLocus} is not a match locus.");

            return locus.Match;
        }

        public static string GetSerologyLocusNameFromMolecular(string molecularLocusName)
        {
            return NamesOfPermittedLoci.First(l => l.Molecular.Equals(molecularLocusName)).Serology;
        }

        public static string GetMolecularLocusNameFromMatch(MatchLocus matchLocusName)
        {
            return NamesOfPermittedLoci.First(l => l.Match.Equals(matchLocusName)).Molecular;
        }
    }
}
