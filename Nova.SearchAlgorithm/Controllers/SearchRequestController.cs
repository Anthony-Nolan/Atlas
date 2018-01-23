using System.Net;
using System.Net.Http;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    public class SearchRequestController : ApiController
    {
        private readonly ISearchRequestService searchRequestService;

        public SearchRequestController(ISearchRequestService creationService)
        {
            searchRequestService = creationService;
        }


        [HttpPost]
        [Route("search-request/create-search-request")]
        public IHttpActionResult CreateSearchRequest([FromBody] SearchRequestCreationModel searchRequestCreationModel)
        {
            var id = searchRequestService.CreateSearchRequest(searchRequestCreationModel);
            return Ok(id.Result);
        }
    }
}
