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
    
    //QQ Migrate to HlaMdDictionary. Rename.
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
        private readonly IActiveHlaVersionAccessor activeHlaVersionProvider;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public MatchingDictionaryService(
            IRecreateHlaLookupResultsService manageMatchingService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IActiveHlaVersionAccessor activeHlaVersionProvider,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.manageMatchingService = manageMatchingService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.activeHlaVersionProvider = activeHlaVersionProvider;//QQ This will be replaced by the value being passed in directly. How does hot swapping work?
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public async Task RecreateMatchingDictionary(CreationBehaviour wmdaHlaVersionToRecreate = CreationBehaviour.Active)
        {
            var version = wmdaHlaVersionToRecreate == CreationBehaviour.Active
                ? activeHlaVersionProvider.GetActiveHlaDatabaseVersion()
                : wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();

            await manageMatchingService.RecreateAllHlaLookupResults(version);
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(locus, alleleLookupName, activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(locus, hlaName, activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(locus, hlaName, activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupLookupService.GetDpb1TceGroup(dpb1HlaName, activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }

        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults(activeHlaVersionProvider.GetActiveHlaDatabaseVersion());
        }
    }
}