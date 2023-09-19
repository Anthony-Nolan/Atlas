using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class HlaInfoForMatching: IHlaInfoToMapAlleleToSerology, IHlaInfoToMapSerologyToAllele
    {
        public List<AlleleInfoForMatching> AlleleInfoForMatching { get; }
        public List<SerologyInfoForMatching> SerologyInfoForMatching { get; }
        public List<RelDnaSer> AlleleToSerologyRelationships { get; }

        public HlaInfoForMatching(
            List<AlleleInfoForMatching> alleleInfoForMatching, 
            List<SerologyInfoForMatching> serologyInfoForMatching, 
            List<RelDnaSer> alleleToSerologyRelationships)
        {
            AlleleInfoForMatching = alleleInfoForMatching;
            SerologyInfoForMatching = serologyInfoForMatching;
            AlleleToSerologyRelationships = alleleToSerologyRelationships;
        }
    }
}
