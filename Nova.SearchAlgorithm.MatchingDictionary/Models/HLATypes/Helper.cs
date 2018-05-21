namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes
{
    /// <summary>
    /// This class is responsible for
    /// describing the static properties of DRB345 serology typings.
    /// </summary>
    public static class Drb345Serologies
    {
        public const string SerologyDrbLocus = "DR";
        public static string[] Drb345Types = { "51", "52", "53" };
    }

    /// <summary>
    /// This class is responsible for
    /// defining static data related to allele expression.
    /// </summary>
    public static class AlleleExpression
    {
        public static string[] NullExpressionSuffixes = { "N" };
    }

    /// <summary>
    /// This class is responsible for
    /// defining static data related to unexpected dna-to-serology mappings.
    /// </summary>
    public static class UnexpectedMappings
    {
        public static HlaType[] PermittedExceptions =
        {
            new HlaType("B", "15"),
            new HlaType("B", "70"),
        };
    }
}
