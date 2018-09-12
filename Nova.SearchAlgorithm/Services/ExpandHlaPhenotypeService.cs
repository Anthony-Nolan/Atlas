using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services
{
    public interface IExpandHlaPhenotypeService
    {
        Task<PhenotypeInfo<ExpandedHla>> GetPhenotypeOfExpandedHla(PhenotypeInfo<string> hlaPhenotype);
    }

    /// <inheritdoc />
    public class ExpandHlaPhenotypeService : IExpandHlaPhenotypeService
    {
        private readonly ILocusHlaMatchingLookupService locusHlaLookupService;

        public ExpandHlaPhenotypeService(ILocusHlaMatchingLookupService locusHlaLookupService)
        {
            this.locusHlaLookupService = locusHlaLookupService;
        }

        public async Task<PhenotypeInfo<ExpandedHla>> GetPhenotypeOfExpandedHla(PhenotypeInfo<string> hlaPhenotype)
        {
            return await hlaPhenotype.WhenAllLoci(GetExpandedHla);
        }

        private async Task<Tuple<ExpandedHla, ExpandedHla>> GetExpandedHla(
            Locus locus,
            string hla1,
            string hla2)
        {
            // TODO:NOVA-1300 figure out how best to lookup matches for Dpb1
            if (locus == Locus.Dpb1 || hla1 == null || hla2 == null)
            {
                return new Tuple<ExpandedHla, ExpandedHla>(null, null);
            }

            var result = await locusHlaLookupService
                .GetHlaMatchingLookupResults(
                    locus.ToMatchLocus(), 
                    new Tuple<string, string>(hla1, hla2));

            return new Tuple<ExpandedHla, ExpandedHla>(
                result.Item1.ToExpandedHla(hla1), 
                result.Item2.ToExpandedHla(hla2));
        }
    }
}