using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// When scoring multiple alleles (e.g. allele string, xx code, nmdp code), we do not want to consider null alleles 
    /// </summary>
    public static class MultipleAlleleNullFilter
    {
        public static IEnumerable<IHlaLookupResultSource<AlleleTyping>> Filter(IEnumerable<IHlaLookupResultSource<AlleleTyping>> resultSource)
        {
            return resultSource.Where(allele => !allele.TypingForHlaLookupResult.IsNullExpresser);
        }

        public static IEnumerable<SingleAlleleScoringInfo> Filter(IEnumerable<SingleAlleleScoringInfo> results)
        {
            return results.Where(info => !ExpressionSuffixParser.IsAlleleNull(info.AlleleName));
        }
    }
}