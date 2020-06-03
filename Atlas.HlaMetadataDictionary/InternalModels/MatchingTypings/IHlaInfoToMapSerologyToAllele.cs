using Atlas.HlaMetadataDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapSerologyToAllele
    {
        List<IAlleleInfoForMatching> AlleleInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
