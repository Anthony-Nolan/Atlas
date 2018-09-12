using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions
{
    public static class LocusExtensions
    {
        public static MatchLocus ToMatchLocus(this Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return MatchLocus.A;
                case Locus.B:
                    return MatchLocus.B;
                case Locus.C:
                    return MatchLocus.C;
                case Locus.Dqb1:
                    return MatchLocus.Dqb1;
                case Locus.Drb1:
                    return MatchLocus.Drb1;
                default:
                    throw new SearchHttpException($"Unable to convert unknown loci {locus} for matching dictionary lookup");
            }
        }
    }
}