using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    public class DictionaryFromSerologySource
    {
        public IEnumerable<MatchingDictionaryEntry> GetDictionaryEntries(IEnumerable<IDictionarySerologySource> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                new MatchingDictionaryEntry(
                    serology.MatchedOnSerology.MatchLocus,
                    serology.MatchedOnSerology.Name,
                    TypingMethod.Serology,
                    MolecularSubtype.NotMolecularType,
                    serology.MatchedOnSerology.SerologySubtype,
                    serology.MatchingPGroups,
                    serology.MatchingSerologies.ToSerologyEntries()
                ));
        }
    }
}