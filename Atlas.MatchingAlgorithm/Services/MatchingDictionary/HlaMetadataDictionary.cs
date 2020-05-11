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
    public interface IHlaMetadataDictionary
    {
        Task<string> RecreateHlaMetadataDictionary(HlaMetadataDictionary.CreationBehaviour wmdaHlaVersionToRecreate);
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName);
        Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName);
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName);
        Task<string> GetDpb1TceGroup(string dpb1HlaName);
        HlaLookupResultCollections GetAllHlaLookupResults();

        /// <summary>
        /// Indicates whether there's a discrepancy between the version of the HLA data that we would use from WMDA,
        /// and the version of the HLA data that was used to pre-process the current Donor data.
        /// </summary>
        /// <returns>True if the versions are different, otherwise false.</returns>
        bool IsRefreshNecessary();
    }

    //QQ Migrate to HlaMdDictionary.
    public class HlaMetadataDictionary: IHlaMetadataDictionary
    {
        public enum CreationBehaviour
        {
            Latest,
            Active
        }

        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private readonly IActiveHlaVersionAccessor activeHlaVersionProvider;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public HlaMetadataDictionary(
            IRecreateHlaMetadataService recreateMetadataService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IActiveHlaVersionAccessor activeHlaVersionProvider,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.recreateMetadataService = recreateMetadataService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.activeHlaVersionProvider = activeHlaVersionProvider;//QQ This will be replaced by the value being passed in directly. How does hot swapping work?
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public bool IsRefreshNecessary()
        {
            var active= activeHlaVersionProvider.GetActiveHlaDatabaseVersion(); 
            var latest = wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();
            return active != latest;
        }

        public async Task<string> RecreateHlaMetadataDictionary(CreationBehaviour wmdaHlaVersionToRecreate)
        {
            var version = wmdaHlaVersionToRecreate == CreationBehaviour.Active
                ? activeHlaVersionProvider.GetActiveHlaDatabaseVersion()
                : wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();

            await recreateMetadataService.RefreshAllHlaMetadata(version);
            return version;
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