using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion
{
    internal static class AlleleTypingExtensions
    {
        public static IEnumerable<string> ToNmdpCodeAlleleLookupNames(this AlleleTyping alleleTyping)
        {
            return new []
            {
                alleleTyping.TwoFieldNameWithExpressionSuffix,
                alleleTyping.TwoFieldNameWithoutExpressionSuffix
            }.Distinct();
        }

        public static string ToXxCodeLookupName(this AlleleTyping alleleTyping)
        {
            return alleleTyping.FirstField;
        }
    }
}
