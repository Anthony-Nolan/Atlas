using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion
{
    internal static class AlleleTypingExtensions
    {
        public static string ToNmdpCodeAlleleLookupName(this AlleleTyping alleleTyping)
        {
            return alleleTyping.TwoFieldName;
        }

        public static string ToXxCodeLookupName(this AlleleTyping alleleTyping)
        {
            return alleleTyping.FirstField;
        }
    }
}
