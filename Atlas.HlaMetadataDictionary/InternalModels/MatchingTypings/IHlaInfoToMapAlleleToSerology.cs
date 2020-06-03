using Atlas.HlaMetadataDictionary.Models.Wmda;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    internal interface IHlaInfoToMapAlleleToSerology
    {
        List<ISerologyInfoForMatching> SerologyInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
