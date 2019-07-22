using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;

namespace Nova.SearchAlgorithm.Services.MatchingDictionary
{
    public interface IExpandHlaPhenotypeService
    {
        Task<PhenotypeInfo<ExpandedHla>> GetPhenotypeOfExpandedHla(PhenotypeInfo<string> hlaPhenotype);
    }

    /// <inheritdoc />
    public class ExpandHlaPhenotypeService : IExpandHlaPhenotypeService
    {
        private readonly ILocusHlaMatchingLookupService locusHlaLookupService;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public ExpandHlaPhenotypeService(ILocusHlaMatchingLookupService locusHlaLookupService, IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.locusHlaLookupService = locusHlaLookupService;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
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
            if (hla1 == null || hla2 == null)
            {
                return new Tuple<ExpandedHla, ExpandedHla>(null, null);
            }

            var result = await locusHlaLookupService
                .GetHlaMatchingLookupResults(locus, new Tuple<string, string>(hla1, hla2), wmdaHlaVersionProvider.GetHlaDatabaseVersion());

            return new Tuple<ExpandedHla, ExpandedHla>(
                result.Item1.ToExpandedHla(hla1), 
                result.Item2.ToExpandedHla(hla2));
        }
    }
}