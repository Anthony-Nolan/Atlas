namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public static class Drb345Serologies
    {
        public const string SerologyDrbLocus = "DR";
        public static string[] Drb345Typings = { "51", "52", "53" };
    }

    public static class AlleleExpression
    {
        public static string[] NullExpressionSuffixes = { "N" };
    }

    public static class UnexpectedDnaToSerologyMappings
    {
        public static HlaTyping[] PermittedExceptions =
        {
            new HlaTyping("B", "15"),
            new HlaTyping("B", "70"),
        };
    }
}
