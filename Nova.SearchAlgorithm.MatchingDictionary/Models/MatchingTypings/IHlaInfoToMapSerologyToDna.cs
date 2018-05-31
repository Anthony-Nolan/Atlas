using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapSerologyToDna
    {
        List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        List<RelDnaSer> DnaToSerologyRelationships { get; }
    }
}
