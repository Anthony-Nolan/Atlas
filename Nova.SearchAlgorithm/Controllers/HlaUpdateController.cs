using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    public class HlaUpdateController : ApiController
    {
        [HttpPost]
        [Route("trigger-hla-update")]
        public IHttpActionResult TriggerSingleImport()
        {
            // TODO: NOVA-929 implement a process to update the HLA of every donor.
            return Ok();
        }
    }
}
