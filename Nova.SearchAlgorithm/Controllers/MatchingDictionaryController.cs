using System.Web.Http;

namespace Nova.SearchAlgorithm.Controllers
{
    public class MatchingDictionaryController : ApiController
    {
        public MatchingDictionaryController()
        {
        }

        [HttpPost]
        [Route("manage")]
        public IHttpActionResult RecreateMatchingDictionary()
        {
            return Ok();
        }
    }
}
