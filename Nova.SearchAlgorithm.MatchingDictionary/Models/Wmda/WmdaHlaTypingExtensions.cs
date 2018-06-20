using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;

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
            return typing.Locus.Equals(Drb345Serologies.Locus) && Drb345Serologies.Typings.Contains(typing.Name);
        }

        public static bool IsPermittedLocusTyping(this IWmdaHlaTyping typing)
        {
            return typing.TypingMethod == TypingMethod.Molecular ?
                PermittedLocusNames.IsPermittedMolecularLocus(typing.Locus) :
                    PermittedLocusNames.IsPermittedSerologyLocus(typing.Locus) && !typing.IsDrb345SerologyTyping();
        }

        public static bool TypingEquals(this IWmdaHlaTyping typing, IWmdaHlaTyping other)
        {
            return typing.Locus.Equals(other.Locus) && typing.Name.Equals(other.Name);
        }

        public static bool LocusEquals(this IWmdaHlaTyping typing, IWmdaHlaTyping other)
        {
            return typing.Locus.Equals(other.Locus);
        }
    }
}
