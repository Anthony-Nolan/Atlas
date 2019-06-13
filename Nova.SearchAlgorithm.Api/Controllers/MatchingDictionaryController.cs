using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Api.Controllers
{
    [Route("matching-dictionary")]
    public class MatchingDictionaryController : ControllerBase
    {
        private readonly IMatchingDictionaryService matchingDictionaryService;

        public MatchingDictionaryController(IMatchingDictionaryService matchingDictionaryService)
        {
            this.matchingDictionaryService = matchingDictionaryService;
        }

        [HttpPost]
        [Route("recreate")]
        public async Task RecreateMatchingDictionary()
        {
            await matchingDictionaryService.RecreateMatchingDictionary();
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