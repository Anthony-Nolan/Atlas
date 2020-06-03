using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class AlleleTypingExtensions
    {
        private const string XxCodeSuffix = ":XX";

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
            return string.Concat(alleleTyping.FirstField, XxCodeSuffix);
        }
    }
}
