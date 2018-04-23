using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    [RoutePrefix("matching-dictionary")]
    public class MatchingDictionaryController : ApiController
    {
        private readonly IManageDictionaryService manageService;
        private readonly IWmdaRepository wmdaRepository;

        public MatchingDictionaryController(IManageDictionaryService service, IWmdaRepository wmdaRepository)
        {
            manageService = service;
            this.wmdaRepository = wmdaRepository;
        }

        [HttpPost]
        [Route("recreate")]
        public IHttpActionResult RecreateMatchingDictionary()
        {
            manageService.RecreateDictionary(wmdaRepository);
            return Ok();
        }
    }
}
