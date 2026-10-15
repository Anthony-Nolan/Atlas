using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IHlaInfoToMapAlleleToSerology
    {
        List<SerologyInfoForMatching> SerologyInfoForMatching { get; }
        ILookup<(string TypingLocus, string Name), RelDnaSer> AlleleToSerologyRelationshipsByTyping { get; }
    }
}
