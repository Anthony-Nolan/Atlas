using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    public class SearchRequestsController : ApiController
    {
        private readonly ISearchRequestService searchRequestService;

        public SearchRequestsController(ISearchRequestService creationService)
        {
            searchRequestService = creationService;
        }

        [HttpPost]
        [Route("search-requests")]
        public IHttpActionResult CreateSearchRequest([FromBody] SearchRequestCreationModel searchRequestCreationModel)
        {
            //todo: NOVA-761 - decide what kind of object to return
            var id = searchRequestService.CreateSearchRequest(searchRequestCreationModel);
            return Ok(id);
        }
    }
}
