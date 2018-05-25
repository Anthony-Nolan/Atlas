using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Services
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
                case MatchLocus.Dqb1:
                    return Locus.Dqb1;
                case MatchLocus.Drb1:
                    return Locus.Drb1;
                default:
                    throw new SearchHttpException($"Unable to convert unknown loci {locus} for matching dictionary lookup");
            }
        }
    }
}