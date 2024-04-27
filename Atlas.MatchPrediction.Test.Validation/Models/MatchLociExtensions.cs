using Atlas.Common.Public.Models.GeneticData;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal static class MatchLociExtensions
    {
        public static string MatchLociToString(this LociInfoTransfer<bool> matchLociInfo)
        {
            var matchLoci = matchLociInfo.ToLociInfo().Reduce(
                (locus, isMatchLocus, loci) =>
                {
                    if (isMatchLocus) loci.Add(locus);
                    return loci;
                },
                new List<Locus>());

            return string.Join(",", matchLoci);
        }
    }
}