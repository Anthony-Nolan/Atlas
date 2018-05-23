using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public static class SerologyEntryExtensions
    {
        public static IEnumerable<SerologyEntry> ToSerologyEntries(this IEnumerable<SerologyTyping> serologyCollection)
        {
            return serologyCollection.Select(s => new SerologyEntry(s.Name, s.SerologySubtype));
        }
    }
}
