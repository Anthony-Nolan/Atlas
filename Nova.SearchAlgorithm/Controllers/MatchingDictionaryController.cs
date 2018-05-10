using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("matching-dictionary")]
    public class MatchingDictionaryController : ApiController
    {
        private readonly IManageDictionaryService manageService;
        private readonly IMatchedHlaRepository dictionaryRepository;

        public MatchingDictionaryController(IManageDictionaryService manageService, IMatchedHlaRepository dictionaryRepository)
        {
            this.manageService = manageService;
            this.dictionaryRepository = dictionaryRepository;
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
        public IHttpActionResult GetMatchedHla(string locus, string lookupTerm, TypingMethod typingMethod)
        {
            var result = dictionaryRepository.GetDictionaryEntry(locus, lookupTerm, typingMethod);
            return Ok(result);
        }
    }
}
