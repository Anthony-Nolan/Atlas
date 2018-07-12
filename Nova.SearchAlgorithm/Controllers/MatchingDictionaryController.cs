using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("matching-dictionary")]
    public class MatchingDictionaryController : ApiController
    {
        private readonly IManageMatchingDictionaryService manageMatchingService;
        private readonly IHlaMatchingLookupService hlaMatchingLookupService;

        public MatchingDictionaryController(
            IManageMatchingDictionaryService manageMatchingService, 
            IHlaMatchingLookupService hlaMatchingLookupService)
        {
            this.manageMatchingService = manageMatchingService;
            this.hlaMatchingLookupService = hlaMatchingLookupService;
        }

        [HttpPost]
        [Route("recreate")]
        public Task RecreateMatchingDictionary()
        {
            return manageMatchingService.RecreateMatchingDictionary();
        }

        [HttpGet]
        [Route("matching-lookup")]
        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(MatchLocus matchLocus, string hlaName)
        {
            return await hlaMatchingLookupService.GetHlaMatchingLookupResult(matchLocus, hlaName);
        }
    }
}
