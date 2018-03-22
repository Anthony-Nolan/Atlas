using System.Collections.Generic;
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
        [Route("search")]
        public IHttpActionResult Search([FromBody] SearchRequestCreationModel searchRequestCreationModel)
        {
            var id = searchRequestService.CreateSearchRequest(searchRequestCreationModel);

            var result = new SearchResultSet
            {
                SearchRequestId = id,
                SearchResults = new List<SearchResult>()
            };

            return Ok(result);
        }
    }
}
