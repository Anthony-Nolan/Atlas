using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.HlaTypingInfo
{
    internal static class UnexpectedAlleleToSerologyMappings
    {
        public static HlaTyping[] PermittedExceptions =
        {
            new HlaTyping(TypingMethod.Serology, "B", "15"),
            new HlaTyping(TypingMethod.Serology, "B", "70")
        };
    }
}
