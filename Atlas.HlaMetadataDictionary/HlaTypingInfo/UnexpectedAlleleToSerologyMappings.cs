using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.HlaTypingInfo
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
