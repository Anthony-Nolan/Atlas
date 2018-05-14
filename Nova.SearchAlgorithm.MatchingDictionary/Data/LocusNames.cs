using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data
{
    public static class LocusNames
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
        
        public static IEnumerable<string> SerologyLoci => Names.Select(n => n.Serology);
        public static IEnumerable<string> MolecularLoci => Names.Select(n => n.Molecular);
        public static IEnumerable<MatchLocus> MatchLoci => Names.Select(n => n.Match);

        private static readonly List<LocusName> Names = new List<LocusName>
        {
            new LocusName("A", "A*", MatchLocus.A),
            new LocusName("B", "B*", MatchLocus.B),
            new LocusName("Cw", "C*", MatchLocus.C),
            new LocusName("DQ", "DQB1*", MatchLocus.Dqb1),
            new LocusName("DR", "DRB1*", MatchLocus.Drb1)
        };

        public static MatchLocus GetMatchLocusFromWmdaLocus(string wmdaLocus)
        {
            var locus = Names.FirstOrDefault(
                    l => l.Molecular.Equals(wmdaLocus) || l.Serology.Equals(wmdaLocus));

            if (locus == null)
                throw new ArgumentException($"{wmdaLocus} is not a match locus.");

            return locus.Match;
        }

        public static string GetSerologyLocusNameFromMolecular(string molecularLocusName)
        {
            return Names.First(l => l.Molecular.Equals(molecularLocusName)).Serology;
        }

        public static string GetMolecularLocusNameFromMatch(MatchLocus matchLocusName)
        {
            return Names.First(l => l.Match.Equals(matchLocusName)).Molecular;
        }
    }
}
