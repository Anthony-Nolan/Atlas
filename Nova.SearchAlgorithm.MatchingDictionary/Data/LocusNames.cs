using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data
{
    public static class LocusNames
    {
        private class LocusName
        {
            public string Serology { get; }
            public string Molecular { get; }
            public string Match { get; }

            public LocusName(string serology, string molecular, string match)
            {
                Serology = serology;
                Molecular = molecular;
                Match = match;
            }
        }
        
        public static IEnumerable<string> SerologyLoci => Names.Select(n => n.Serology);
        public static IEnumerable<string> MolecularLoci => Names.Select(n => n.Molecular);
        public static IEnumerable<string> MatchLoci => Names.Select(n => n.Match);

        private static readonly List<LocusName> Names = new List<LocusName>
        {
            new LocusName("A", "A*", "A"),
            new LocusName("B", "B*", "B"),
            new LocusName("Cw", "C*", "C"),
            new LocusName("DQ", "DQB1*", "DQB1"),
            new LocusName("DR", "DRB1*", "DRB1")
        };

        public static string GetMatchLocusFromWmdaLocus(string wmdaLocus)
        {
            return Names.FirstOrDefault(
                l => l.Molecular.Equals(wmdaLocus) || l.Serology.Equals(wmdaLocus))
                ?.Match;
        }

        public static string GetSerologyLocusNameFromMolecular(string molecularLocusName)
        {
            return Names.First(l => l.Molecular.Equals(molecularLocusName)).Serology;
        }

        public static string GetMolecularLocusNameFromMatch(string matchLocusName)
        {
            return Names.First(l => l.Match.Equals(matchLocusName)).Molecular;
        }
    }
}
