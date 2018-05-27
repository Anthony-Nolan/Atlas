using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public static class Drb345Typings
    {
        private const string SerologyLocus = "DR";
        private static readonly string[] SerologyTypings = { "51", "52", "53" };

        public static bool IsDrb345SerologyTyping(IWmdaHlaTyping typing)
        {
            return typing.WmdaLocus.Equals(SerologyLocus) && SerologyTypings.Contains(typing.Name);
        }
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
