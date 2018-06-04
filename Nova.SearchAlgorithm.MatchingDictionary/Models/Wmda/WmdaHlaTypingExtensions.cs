using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    internal static class WmdaHlaTypingExtensions
    {
        private static class Drb345Serologies
        {
            public const string Locus = "DR";
            public static readonly string[] Typings = { "51", "52", "53" };
        }

        public static bool IsDrb345SerologyTyping(this IWmdaHlaTyping typing)
        {
            return typing.WmdaLocus.Equals(Drb345Serologies.Locus) && Drb345Serologies.Typings.Contains(typing.Name);
        }
    }
}
