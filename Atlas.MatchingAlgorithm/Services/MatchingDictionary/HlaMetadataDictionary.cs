using System;
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
        Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetLocusHlaMatchingLookupResults(Locus locus, Tuple<string, string> locusTyping);
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

        private readonly HlaMetadataConfiguration config;
        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly ILocusHlaMatchingLookupService locusHlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public HlaMetadataDictionary(
            HlaMetadataConfiguration config,
            IRecreateHlaMetadataService recreateMetadataService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            ILocusHlaMatchingLookupService locusHlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.config = config;
            this.recreateMetadataService = recreateMetadataService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.locusHlaMatchingLookupService = locusHlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public bool IsRefreshNecessary()
        {
            var active= config.ActiveWmdaVersion; 
            var latest = wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();
            return active != latest;
        }

        public async Task<string> RecreateHlaMetadataDictionary(CreationBehaviour wmdaHlaVersionToRecreate)
        {
            var version = wmdaHlaVersionToRecreate == CreationBehaviour.Active
                ? config.ActiveWmdaVersion
                : wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();

            await recreateMetadataService.RefreshAllHlaMetadata(version);  //QQ actually needs to pass BOTH the whole object AND the target version string. !Separately! (Or maybe just update config? idk. TBC).
            return version;
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(locus, alleleLookupName, config.ActiveWmdaVersion);  //QQ actually needs to pass the whole object. Etc. below.
        }

        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(locus, hlaName, config.ActiveWmdaVersion);
        }

        public async Task<Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>> GetLocusHlaMatchingLookupResults(Locus locus, Tuple<string, string> locusTyping)
        {
            return await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(locus, locusTyping, config.ActiveWmdaVersion);
        }

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(locus, hlaName, config.ActiveWmdaVersion);
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupLookupService.GetDpb1TceGroup(dpb1HlaName, config.ActiveWmdaVersion);
        }

        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults(config.ActiveWmdaVersion);
        }
    }
}