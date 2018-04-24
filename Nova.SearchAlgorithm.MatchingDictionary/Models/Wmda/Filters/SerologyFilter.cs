using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters
{
    public sealed class SerologyFilter : MatchLociFilter
    {
        public static SerologyFilter Instance { get; } = new SerologyFilter();

        private SerologyFilter()
        {
            MatchLoci = new List<string>(
                LocusNames.SerologyLoci.Where(locus => !locus.Equals(Drb345Serologies.SerologyDrbLocus)));

            Filter = entry => MatchLoci.Contains(entry.WmdaLocus)
                || (entry.WmdaLocus.Equals(Drb345Serologies.SerologyDrbLocus)
                    && !Drb345Serologies.Drb345Types.Contains(entry.Name));
        }
    }
}
