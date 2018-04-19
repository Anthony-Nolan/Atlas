using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters
{
    public sealed class SerologyFilter : MatchLociFilter
    {
        public static SerologyFilter Instance { get; } = new SerologyFilter();

        private SerologyFilter()
        {
            MatchLoci = new List<string> { "A", "B", "Cw", "DQ" };
            Filter = entry => MatchLoci.Contains(entry.WmdaLocus)
                || (entry.WmdaLocus.Equals(Drb345Serologies.SerologyDrbLocus)
                    && !Drb345Serologies.Drb345Types.Contains(entry.Name));
        }
    }
}
