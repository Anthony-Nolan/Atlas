using System;
using Atlas.Common.Public.Models.GeneticData;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Validation.Models
{
    internal static class MatchLociExtensions
    {
        private const string MatchLociSeparator = ",";

        public static string MatchLociToString(this LociInfoTransfer<bool> matchLociInfo)
        {
            var matchLoci = matchLociInfo.ToLociInfo().Reduce(
                (locus, isMatchLocus, loci) =>
                {
                    if (isMatchLocus) loci.Add(locus);
                    return loci;
                },
                new List<Locus>());

            return string.Join(MatchLociSeparator, matchLoci);
        }

        public static LociInfo<bool> ToLociInfo(this string matchLoci)
        {
            return matchLoci
                .Split(MatchLociSeparator)
                .Aggregate(
                    new LociInfo<bool>(false),
                    (currentLociInfo, locus) => currentLociInfo.SetLocus(Enum.Parse<Locus>(locus), true));
        }

        public static ISet<Locus> ToSet(this string matchLoci)
        {
            return matchLoci
                .Split(MatchLociSeparator)
                .Select(Enum.Parse<Locus>)
                .ToHashSet();
        }
    }
}