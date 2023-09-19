using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IHlaInfoToMapAlleleToSerology
    {
        List<SerologyInfoForMatching> SerologyInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
