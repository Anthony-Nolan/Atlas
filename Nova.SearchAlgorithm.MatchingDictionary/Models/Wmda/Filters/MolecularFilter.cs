using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters
{
    public sealed class MolecularFilter : MatchLociFilter
    {
        public static MolecularFilter Instance { get; } = new MolecularFilter();

        private MolecularFilter()
        {
            MatchLoci = new List<string> { "A*", "B*", "C*", "DQB1*", "DRB1*" };
            Filter = entry => MatchLoci.Contains(entry.WmdaLocus);
        }
    }
}
