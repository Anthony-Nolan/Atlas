using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class MatchedAllele : IMatchedHla, IHlaLookupResultSource<AlleleTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public IEnumerable<SerologyMappingForAllele> AlleleToSerologyMappings { get; }
        public AlleleTyping TypingForHlaLookupResult => (AlleleTyping) HlaTyping;

        public MatchedAllele(IAlleleInfoForMatching matchedAllele, IEnumerable<SerologyMappingForAllele> alleleToSerologyMappings)
        {
            HlaTyping = matchedAllele.HlaTyping;
            TypingUsedInMatching = matchedAllele.TypingUsedInMatching;
            MatchingPGroups = matchedAllele.MatchingPGroups;
            MatchingGGroups = matchedAllele.MatchingGGroups;

            var alleleToSerologyMappingsCollection = alleleToSerologyMappings.ToList();
            MatchingSerologies = alleleToSerologyMappingsCollection.SelectMany(ConvertMappingToMatchingSerology);

            // TODO: NOVA-1483 - Do not need to store all details of allele-to-serology mappings
            AlleleToSerologyMappings = alleleToSerologyMappingsCollection;
        }

        private static IEnumerable<MatchingSerology> ConvertMappingToMatchingSerology(SerologyMappingForAllele mapping)
        {
            return mapping
                .AllMatchingSerology
                .Select(ser => ser.SerologyTyping)
                .Select(ser => new MatchingSerology(ser, ser.Equals(mapping.DirectSerology)));
        }
    }
}
