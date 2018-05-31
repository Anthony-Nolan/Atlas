using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo
{
    internal static class UnexpectedAlleleToSerologyMappings
    {
        public static HlaTyping[] PermittedExceptions =
        {
            new HlaTyping("B", "15"),
            new HlaTyping("B", "70"),
        };
    }
}
