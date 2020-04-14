using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapSerologyToAllele
    {
        List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
