namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    public static class Drb345Serologies
    {
        public const string SerologyDrbLocus = "DR";
        public static string[] Drb345Types = { "51", "52", "53" };
    }

    public static class AlleleExpression
    {
        public static string[] NullExpressionSuffixes = { "N" };
    }

    public static class UnexpectedMappings
    {
        public static HlaType[] AcceptableSerologies =
        {
            new HlaType("B", "15"),
            new HlaType("B", "70"),
        };
    }
}
