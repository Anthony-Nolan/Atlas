using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions
{
    static class MatchLocusExtensions
    {
        public static Locus ToLocus(this MatchLocus locus)
        {
            switch (locus)
            {
                case MatchLocus.A:
                    return Locus.A;
                case MatchLocus.B:
                    return Locus.B;
                case MatchLocus.C:
                    return Locus.C;
                case MatchLocus.Dpb1:
                    return Locus.Dpb1;
                case MatchLocus.Dqb1:
                    return Locus.Dqb1;
                case MatchLocus.Drb1:
                    return Locus.Drb1;
                default:
                    throw new SearchHttpException($"Unable to convert unknown locus {locus} for matching dictionary lookup");
            }
        }
    }
}