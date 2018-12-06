using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("matching-dictionary")]
    public class MatchingDictionaryController : ApiController
    {
        private readonly IRecreateHlaLookupResultsService manageMatchingService;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IDpb1TceGroupLookupService dpb1TceGroupLookupService;

        public MatchingDictionaryController(
            IRecreateHlaLookupResultsService manageMatchingService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupLookupService dpb1TceGroupLookupService)
        {
            this.manageMatchingService = manageMatchingService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupLookupService = dpb1TceGroupLookupService;
        }

        [HttpPost]
        [Route("recreate")]
        public Task RecreateMatchingDictionary()
        {
            return manageMatchingService.RecreateAllHlaLookupResults();
        }

        [HttpGet]
        [Route("allele-names-lookup")]
        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(locus, alleleLookupName);
        }

        [HttpGet]
        [Route("matching-lookup")]
        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(
            Locus locus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("scoring-lookup")]
        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(
            Locus locus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(locus, hlaName);
        }

        [HttpGet]
        [Route("dpb1-tce-group-lookup")]
        public async Task<string> GetDpb1TceGroup(string dpb1HlaName)
        {
            return await dpb1TceGroupLookupService.GetDpb1TceGroup(dpb1HlaName);
        }

        [HttpGet]
        [Route("all-results")]
        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults();
        }
    }
}
