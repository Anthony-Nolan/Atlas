namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    internal static class AlleleExpression
    {
        public static string[] NullExpressionSuffixes = { "N" };
    }

    internal static class UnexpectedDnaToSerologyMappings
    {
        public static HlaTyping[] PermittedExceptions =
        {
            new HlaTyping("B", "15"),
            new HlaTyping("B", "70"),
        };
    }
}
