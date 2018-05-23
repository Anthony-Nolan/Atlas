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
        private readonly IMatchingDictionaryLookupService lookupService;

        public MatchingDictionaryController(IManageMatchingDictionaryService manageMatchingService, IMatchingDictionaryLookupService lookupService)
        {
            this.manageMatchingService = manageMatchingService;
            this.lookupService = lookupService;
        }

        [HttpPost]
        [Route("recreate")]
        public IHttpActionResult RecreateMatchingDictionary()
        {
            manageMatchingService.RecreateMatchingDictionary();
            return Ok();
        }

        [HttpGet]
        [Route("lookup")]
        //todo: NOVA-1201 - delete this endpoint; only used for demo/testing, not required for production
        public async Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            return await lookupService.GetMatchingHla(matchLocus, hlaName);
        }
    }
}
