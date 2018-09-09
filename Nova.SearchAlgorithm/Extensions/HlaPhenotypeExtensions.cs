using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;

namespace Nova.SearchAlgorithm.Extensions
{
    public static class HlaPhenotypeExtensions
    {
        public static async Task<PhenotypeInfo<ExpandedHla>> ToExpandedHlaPhenotype(
            this PhenotypeInfo<string> hlas,
            ILocusHlaMatchingLookupService matchingLookupService)
        {
            return await hlas.WhenAllLoci((l, h1, h2) => GetExpandedHla(l, h1, h2, matchingLookupService));
        }

        private static async Task<Tuple<ExpandedHla, ExpandedHla>> GetExpandedHla(
            Locus locus,
            string hla1, 
            string hla2, 
            ILocusHlaMatchingLookupService matchingLookupService)
        {
            // TODO:NOVA-1300 figure out how best to lookup matches for Dpb1
            if (locus == Locus.Dpb1 || hla1 == null || hla2 == null)
            {
                return new Tuple<ExpandedHla, ExpandedHla>(null, null);
            }

            var result = await matchingLookupService
                .GetHlaMatchingLookupResultForLocus(locus.ToMatchLocus(), new Tuple<string, string>(hla1, hla2));

            return new Tuple<ExpandedHla, ExpandedHla>(result.Item1.ToExpandedHla(hla1), result.Item2.ToExpandedHla(hla2));
        }
    }
}
