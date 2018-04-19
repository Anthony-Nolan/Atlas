using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
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
    }
}
