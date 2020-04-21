using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Services.MatchingDictionary;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    [Route("matching-dictionary")]
    public class MatchingDictionaryController : ControllerBase
    {
        private readonly IMatchingDictionaryService matchingDictionaryService;

        public MatchingDictionaryController(
            IMatchingDictionaryService matchingDictionaryService)
        {
            this.matchingDictionaryService = matchingDictionaryService;
        }

        [HttpPost]
        [Route("create-latest-version")]
        public async Task CreateLatestMatchingDictionary()
        {
            await matchingDictionaryService.RecreateMatchingDictionary(MatchingDictionaryService.CreationBehaviour.Latest);
        }

        [HttpPost]
        [Route("recreate-active-version")]
        public async Task RecreateActiveMatchingDictionary()
        {
            await matchingDictionaryService.RecreateMatchingDictionary(MatchingDictionaryService.CreationBehaviour.Active);
        }

        [HttpGet]
        [Route("allele-names-lookup")]
        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await matchingDictionaryService.GetCurrentAlleleNames(locus, alleleLookupName);
        }

        [HttpGet]
        [Route("matching-lookup")]
        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(
            Locus locus,
            string hlaName)
        {
            return await matchingDictionaryService.GetHlaMatchingLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("scoring-lookup")]
        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(
            Locus locus,
            string hlaName)
        {
            return await matchingDictionaryService.GetHlaScoringLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("dpb1-tce-group-lookup")]
        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await matchingDictionaryService.GetDpb1TceGroup(dpb1HlaName);
        }

        [HttpGet]
        [Route("all-results")]
        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return matchingDictionaryService.GetAllHlaLookupResults();
        }
    }
}