using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    public interface IHlaMetadataDictionary
    {
        string ActiveVersion();
        Task<string> RecreateHlaMetadataDictionary(CreationBehaviour wmdaHlaVersionToRecreate);
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName);
        Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName);
        Task<LocusInfo<IHlaMatchingLookupResult>> GetLocusHlaMatchingLookupResults(Locus locus, LocusInfo<string> locusTyping);
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName);
        Task<string> GetDpb1TceGroup(string dpb1HlaName);
        IEnumerable<string> GetAllPGroups();
        HlaLookupResultCollections GetAllHlaLookupResults();

        /// <summary>
        /// Indicates whether there's a discrepancy between the version of the HLA data that we would use from WMDA,
        /// and the version of the HLA data that was used to pre-process the current Donor data.
        /// </summary>
        /// <returns>True if the versions are different, otherwise false.</returns>
        bool IsActiveVersionDifferentFromLatestVersion();
    }

    internal class HlaMetadataDictionary: IHlaMetadataDictionary
    {
        private readonly string activeHlaNomenclatureVersion;
        private readonly IRecreateHlaMetadataService recreateMetadataService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly ILocusHlaMatchingLookupService locusHlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public HlaMetadataDictionary(
            string activeHlaNomenclatureVersion,
            IRecreateHlaMetadataService recreateMetadataService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            ILocusHlaMatchingLookupService locusHlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService,
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.activeHlaNomenclatureVersion = activeHlaNomenclatureVersion;
            this.recreateMetadataService = recreateMetadataService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.locusHlaMatchingLookupService = locusHlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        public bool IsActiveVersionDifferentFromLatestVersion()
        {
            var active = activeHlaNomenclatureVersion; 
            var latest = wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();
            return active != latest;
        }

        public string ActiveVersion()
        {
            return activeHlaNomenclatureVersion;
        }

        public async Task<string> RecreateHlaMetadataDictionary(CreationBehaviour creationConfig)
        {
            var version = IdentifyVersionToRecreate(creationConfig);
            await recreateMetadataService.RefreshAllHlaMetadata(version);
            return version;
        }

        private string IdentifyVersionToRecreate(CreationBehaviour creationConfig)
        {
            switch (creationConfig.CreationMode)
            {
                case CreationBehaviour.Mode.Specific:
                    return creationConfig.SpecificVersion;
                case CreationBehaviour.Mode.Active:
                    return activeHlaNomenclatureVersion;
                case CreationBehaviour.Mode.Latest:
                    return wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion();
                default:
                    throw new ArgumentOutOfRangeException(nameof(creationConfig.CreationMode), creationConfig.CreationMode, "Unexpected enum value");
            }
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(locus, alleleLookupName, activeHlaNomenclatureVersion);
        }

        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(Locus locus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(locus, hlaName, activeHlaNomenclatureVersion);
        }

        public async Task<LocusInfo<IHlaMatchingLookupResult>> GetLocusHlaMatchingLookupResults(Locus locus, LocusInfo<string> locusTyping)
        {
            return await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(locus, locusTyping, activeHlaNomenclatureVersion);
        }

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(Locus locus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(locus, hlaName, activeHlaNomenclatureVersion);
        }

        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupLookupService.GetDpb1TceGroup(dpb1HlaName, activeHlaNomenclatureVersion);
        }

        public IEnumerable<string> GetAllPGroups()
        {
            return hlaMatchingLookupService.GetAllPGroups(activeHlaNomenclatureVersion);
        }

        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults(activeHlaNomenclatureVersion);
        }
    }
}