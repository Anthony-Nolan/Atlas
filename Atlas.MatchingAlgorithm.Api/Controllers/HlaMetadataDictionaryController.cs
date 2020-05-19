using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("hla-metadata-dictionary")] //TODO: ATLAS-262 (MDM) migrate to new project
    public class HlaMetadataDictionaryController : ControllerBase
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public HlaMetadataDictionaryController(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaVersionAccessor hlaVersionAccessor)
        {
            this.hlaMetadataDictionary = factory.BuildDictionary(hlaVersionAccessor.GetActiveHlaDatabaseVersion());
        }

        [HttpPost]
        [Route("create-latest-version")]
        public async Task CreateLatestHlaMetadataDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(HlaMetadataDictionary.HlaMetadataDictionary.CreationBehaviour.Latest);
        }

        [HttpPost]
        [Route("recreate-active-version")]
        public async Task RecreateActiveHlaMetadataDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(HlaMetadataDictionary.HlaMetadataDictionary.CreationBehaviour.Active);
        }

        [HttpGet]
        [Route("allele-names-lookup")]
        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await hlaMetadataDictionary.GetCurrentAlleleNames(locus, alleleLookupName);
        }

        [HttpGet]
        [Route("matching-lookup")]
        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(
            Locus locus,
            string hlaName)
        {
            return await hlaMetadataDictionary.GetHlaMatchingLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("scoring-lookup")]
        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(
            Locus locus,
            string hlaName)
        {
            return await hlaMetadataDictionary.GetHlaScoringLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("dpb1-tce-group-lookup")]
        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await hlaMetadataDictionary.GetDpb1TceGroup(dpb1HlaName);
        }

        [HttpGet]
        [Route("all-results")]
        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaMetadataDictionary.GetAllHlaLookupResults();
        }
    }
}