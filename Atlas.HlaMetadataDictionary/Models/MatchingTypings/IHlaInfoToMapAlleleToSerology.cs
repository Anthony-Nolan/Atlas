using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapAlleleToSerology
    {
        List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
