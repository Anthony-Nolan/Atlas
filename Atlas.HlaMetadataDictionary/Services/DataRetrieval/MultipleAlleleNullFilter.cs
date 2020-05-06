using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.HlaMetadataDictionary.Services
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