using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IHlaInfoToMapSerologyToAllele
    {
        List<AlleleInfoForMatching> AlleleInfoForMatching { get; }
        List<RelDnaSer> AlleleToSerologyRelationships { get; }
    }
}
