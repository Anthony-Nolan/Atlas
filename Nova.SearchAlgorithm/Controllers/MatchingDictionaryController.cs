using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
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
        private readonly IDpb1TceGroupsLookupService dpb1TceGroupsLookupService;

        public MatchingDictionaryController(
            IRecreateHlaLookupResultsService manageMatchingService,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaMatchingLookupService hlaMatchingLookupService,
            IHlaScoringLookupService hlaScoringLookupService,
            IHlaLookupResultsService hlaLookupResultsService,
            IDpb1TceGroupsLookupService dpb1TceGroupsLookupService)
        {
            this.manageMatchingService = manageMatchingService;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.dpb1TceGroupsLookupService = dpb1TceGroupsLookupService;
        }

        [HttpPost]
        [Route("recreate")]
        public Task RecreateMatchingDictionary()
        {
            return manageMatchingService.RecreateAllHlaLookupResults();
        }

        [HttpGet]
        [Route("allele-names-lookup")]
        public async Task<IEnumerable<string>> GetCurrentAlleleNames(MatchLocus matchLocus, string alleleLookupName)
        {
            return await alleleNamesLookupService.GetCurrentAlleleNames(matchLocus, alleleLookupName);
        }

        [HttpGet]
        [Route("matching-lookup")]
        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(
            MatchLocus matchLocus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaLookupResult(matchLocus, hlaName);
        }

        [HttpGet]
        [Route("scoring-lookup")]
        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResult(
            MatchLocus matchLocus, string hlaName)
        {
            return await hlaScoringLookupService.GetHlaLookupResult(matchLocus, hlaName);
        }

        [HttpGet]
        [Route("dpb1-tce-groups-lookup")]
        public async Task<IEnumerable<string>> GetDpb1TceGroupsLookupResult(
            string dpb1HlaName)
        {
            return await dpb1TceGroupsLookupService.GetDpb1TceGroups(dpb1HlaName);
        }

        [HttpGet]
        [Route("all-results")]
        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            return hlaLookupResultsService.GetAllHlaLookupResults();
        }
    }
}
