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
        public Task RecreateMatchingDictionary()
        {
            return manageMatchingService.RecreateMatchingDictionary();
        }

        [HttpGet]
        [Route("lookup")]
        public async Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            return await lookupService.GetMatchingHla(matchLocus, hlaName);
        }
    }
}
