using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    internal static class SerologyEntryExtensions
    {
        internal static IEnumerable<SerologyEntry> ToSerologyEntries(this IEnumerable<Serology> serologyCollection)
        {
            return serologyCollection.Select(s => new SerologyEntry(s.Name, s.SerologySubtype));
        }
    }
}
