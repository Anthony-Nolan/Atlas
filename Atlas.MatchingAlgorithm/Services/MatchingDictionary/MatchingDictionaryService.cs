using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;

namespace Atlas.MatchingAlgorithm.Services.MatchingDictionary
{
    public interface IMatchingDictionaryService
    {
        Task RecreateMatchingDictionary(MatchingDictionaryService.CreationBehaviour wmdaHlaVersionToRecreate = MatchingDictionaryService.CreationBehaviour.Active);
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName);
        Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName);
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName);
        Task<string> GetDpb1TceGroup(string dpb1HlaName);
        HlaLookupResultCollections GetAllHlaLookupResults();
    }
    
    public class MatchingDictionaryService: IMatchingDictionaryService
    {
        public enum CreationBehaviour
        {
            Latest,
            Active
        }

        private readonly IRecreateHlaLookupResultsService manageMatchingService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public MatchingDictionaryService(
            IRecreateHlaLookupResultsService manageMatchingService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.manageMatchingService = manageMatchingService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public async Task RecreateMatchingDictionary(CreationBehaviour wmdaHlaVersionToRecreate = CreationBehaviour.Active)
        {
            var version = wmdaHlaVersionToRecreate == CreationBehaviour.Active
                ? wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion()
                : wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();

            await manageMatchingService.RecreateAllHlaLookupResults(version);
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(locus, alleleLookupName, wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(locus, hlaName, wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(locus, hlaName, wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupLookupService.GetDpb1TceGroup(dpb1HlaName, wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults(wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }
    }
}