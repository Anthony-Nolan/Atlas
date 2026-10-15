using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class HlaInfoForMatching: IHlaInfoToMapAlleleToSerology, IHlaInfoToMapSerologyToAllele
    {
        public List<AlleleInfoForMatching> AlleleInfoForMatching { get; }
        public List<SerologyInfoForMatching> SerologyInfoForMatching { get; }
        public ILookup<(string TypingLocus, string Name), RelDnaSer> AlleleToSerologyRelationshipsByTyping { get; }

        public HlaInfoForMatching(
            List<AlleleInfoForMatching> alleleInfoForMatching,
            List<SerologyInfoForMatching> serologyInfoForMatching,
            List<RelDnaSer> alleleToSerologyRelationships)
        {
            AlleleInfoForMatching = alleleInfoForMatching;
            SerologyInfoForMatching = serologyInfoForMatching;
            // Indexed once here: the relationships of an allele are looked up for every allele, and every serology.
            AlleleToSerologyRelationshipsByTyping = alleleToSerologyRelationships.ToLookup(r => (r.TypingLocus, r.Name));
        }
    }
}
