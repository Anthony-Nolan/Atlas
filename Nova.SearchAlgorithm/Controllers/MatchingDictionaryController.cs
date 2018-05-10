using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
using System.Web.Http;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("matching-dictionary")]
    public class MatchingDictionaryController : ApiController
    {
        private readonly IManageDictionaryService manageService;
        private readonly IDictionaryLookupService lookupService;

        public MatchingDictionaryController(IManageDictionaryService manageService, IDictionaryLookupService lookupService)
        {
            this.manageService = manageService;
            this.lookupService = lookupService;
        }

        [HttpPost]
        [Route("recreate")]
        public IHttpActionResult RecreateMatchingDictionary()
        {
            manageService.RecreateDictionary();
            return Ok();
        }

        [HttpGet]
        [Route("lookup")]
        public async Task<MatchingDictionaryEntry> GetMatchedHla(string locus, string hlaName)
        {
            return await lookupService.GetMatchedHla(locus, hlaName);
        }
    }
}
