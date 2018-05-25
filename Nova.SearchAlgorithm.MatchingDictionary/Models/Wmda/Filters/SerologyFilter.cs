using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
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
                    && !Drb345Serologies.Drb345Typings.Contains(entry.Name));
        }
    }
}
