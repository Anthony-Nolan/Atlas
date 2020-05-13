using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

//QQ this whole class is moving into HlaMdDict
namespace Atlas.MatchingAlgorithm.Services.MatchingDictionary
{
    public interface IExpandHlaPhenotypeService
    {
        Task<PhenotypeInfo<IHlaMatchingLookupResult>> GetPhenotypeOfExpandedHla(PhenotypeInfo<string> hlaPhenotype, string hlaDatabaseVersion);
    }

    /// <inheritdoc />
    public class ExpandHlaPhenotypeService : IExpandHlaPhenotypeService
    {
        private readonly ILocusHlaMatchingLookupService locusHlaLookupService;
        private readonly IActiveHlaVersionAccessor activeHlaVersionProvider;

        public ExpandHlaPhenotypeService(
            ILocusHlaMatchingLookupService locusHlaLookupService,
            IActiveHlaVersionAccessor activeHlaVersionProvider)
        {
            this.locusHlaLookupService = locusHlaLookupService;
            this.activeHlaVersionProvider = activeHlaVersionProvider;
        }

        public async Task<PhenotypeInfo<IHlaMatchingLookupResult>> GetPhenotypeOfExpandedHla(PhenotypeInfo<string> hlaPhenotype, string hlaDatabaseVersion)
        {
            if (hlaDatabaseVersion == null)
            {
                hlaDatabaseVersion = activeHlaVersionProvider.GetActiveHlaDatabaseVersion();
            }

            return await hlaPhenotype.WhenAllLoci((l, h1, h2) => GetExpandedHla(l, h1, h2, hlaDatabaseVersion));
        }

        private async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetExpandedHla(Locus locus, string hla1, string hla2, string hlaDatabaseVersion)
        {
            if (string.IsNullOrEmpty(hla1) || string.IsNullOrEmpty(hla2))
            {
                return new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(null, null);
            }

            return await locusHlaLookupService.GetHlaMatchingLookupResults(locus, new Tuple<string, string>(hla1, hla2), hlaDatabaseVersion);
        }
    }
}