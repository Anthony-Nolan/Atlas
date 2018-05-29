using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapSerologyToAllele
    {
        List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        List<RelDnaSer> DnaToSerologyRelationships { get; }
    }
}
