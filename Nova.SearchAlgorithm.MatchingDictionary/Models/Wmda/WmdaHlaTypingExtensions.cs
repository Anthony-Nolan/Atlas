using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    internal static class WmdaHlaTypingExtensions
    {
        public static bool IsDrb345SerologyTyping(this IWmdaHlaTyping typing)
        {
            return typing.WmdaLocus.Equals(Drb345Typings.SerologyLocus) && Drb345Typings.SerologyTypings.Contains(typing.Name);
        }
    }
}
