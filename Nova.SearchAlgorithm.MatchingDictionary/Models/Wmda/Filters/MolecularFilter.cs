using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Data;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters
{
    public sealed class MolecularFilter : MatchLociFilter
    {
        public static MolecularFilter Instance { get; } = new MolecularFilter();

        private MolecularFilter()
        {
            MatchLoci = new List<string> (LocusNames.MolecularLoci);
            Filter = entry => MatchLoci.Contains(entry.WmdaLocus);
        }
    }
}
