using Atlas.Utils.Hla.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    public interface IHlaSearchingLookupService<THlaLookupResult>
        where THlaLookupResult : IHlaLookupResult
    {
        Task<THlaLookupResult> GetHlaLookupResult(Locus locus, string hlaName, string hlaDatabaseVersion);
    }

    /// <summary>
    /// Common functionality used when querying a HLA 'searching' 
    /// (i.e., matching or scoring) lookup repository.
    /// </summary>
    public abstract class HlaSearchingLookupServiceBase<THlaLookupResult> :
        LookupServiceBase<THlaLookupResult>,
        IHlaSearchingLookupService<THlaLookupResult>
        where THlaLookupResult : IHlaLookupResult
    {
        protected readonly IHlaCategorisationService HlaCategorisationService;

        private readonly IHlaLookupRepository hlaLookupRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly INmdpCodeCache cache;

        protected HlaSearchingLookupServiceBase(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache
        )
        {
            this.hlaLookupRepository = hlaLookupRepository;
            this.alleleNamesLookupService = alleleNamesLookupService;
            HlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.cache = cache;
        }

        public async Task<THlaLookupResult> GetHlaLookupResult(Locus locus, string hlaName, string hlaDatabaseVersion)
        {
            return await GetLookupResults(locus, hlaName, hlaDatabaseVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<THlaLookupResult> PerformLookup(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            return await GetSingleHlaLookupResult(locus, lookupName, hlaDatabaseVersion);
        }

        private async Task<THlaLookupResult> GetSingleHlaLookupResult(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var dictionaryLookup = GetHlaLookup(lookupName);
            var lookupTableEntities = await dictionaryLookup.PerformLookupAsync(locus, lookupName, hlaDatabaseVersion);
            var lookupResults = ConvertTableEntitiesToLookupResults(lookupTableEntities);

            return ConsolidateHlaLookupResults(locus, lookupName, lookupResults);
        }

        private HlaLookupBase GetHlaLookup(string lookupName)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);

            return HlaLookupFactory
                .GetLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    hlaLookupRepository,
                    alleleNamesLookupService,
                    alleleSplitter,
                    cache);
        }

        protected abstract IEnumerable<THlaLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> hlaLookupTableEntities);

        protected abstract THlaLookupResult ConsolidateHlaLookupResults(
            Locus locus,
            string lookupName,
            IEnumerable<THlaLookupResult> lookupResults);
    }
}