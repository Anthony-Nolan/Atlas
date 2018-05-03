using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    public class DictionaryFromSerology
    {
        public IEnumerable<MatchingDictionaryEntry> GetDictionaryEntries(IEnumerable<IMatchedHla> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                new MatchingDictionaryEntry(
                    serology.HlaType.MatchLocus,
                    serology.HlaType.Name,
                    TypingMethod.Serology,
                    MolecularSubtype.NotMolecularType,
                    ((Serology)serology.HlaType).SerologySubtype,
                    serology.MatchingPGroups,
                    serology.MatchingSerologies.ToSerologyEntries()
                ));
        }
    }
}