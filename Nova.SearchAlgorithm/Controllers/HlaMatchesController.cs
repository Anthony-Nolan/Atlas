using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Hla;

namespace Nova.SearchAlgorithm.Controllers
{
    /// <summary>
    /// Some simple routes for testing and querying the HLA matching dictionary.
    /// This may also be useful for verification.
    /// </summary>
    [Route("hla")]
    public class HlaMatchesController : ApiController
    {
        private readonly IHlaRepository hlaRepository;

        public HlaMatchesController(IHlaRepository hlaRepository)
        {
            this.hlaRepository = hlaRepository;
        }

        [HttpGet]
        [Route("match")]
        public IHttpActionResult Search(string locusName, string hlaName)
        {
            var result = hlaRepository.RetrieveHlaMatches(locusName, hlaName);

            return Ok(result);
        }
    }
}
