using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("matching-dictionary")]
    public class MatchingDictionaryController : ControllerBase
    {
        private readonly IHlaMetadataDictionary hlaMetadataDictionary;

        public MatchingDictionaryController(
            IHlaMetadataDictionary hlaMetadataDictionary)
        {
            this.hlaMetadataDictionary = hlaMetadataDictionary;
        }

        [HttpPost]
        [Route("create-latest-version")]
        public async Task CreateLatestMatchingDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(Services.MatchingDictionary.HlaMetadataDictionary.CreationBehaviour.Latest);
        }

        [HttpPost]
        [Route("recreate-active-version")]
        public async Task RecreateActiveMatchingDictionary()
        {
            await hlaMetadataDictionary.RecreateHlaMetadataDictionary(Services.MatchingDictionary.HlaMetadataDictionary.CreationBehaviour.Active);
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