using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Nova.Utils.Http.Exceptions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Controllers
{
    public class SearchRequestsController : ApiController
    {
        private readonly ISearchService searchService;

        public SearchRequestsController(ISearchService search)
        {
            searchService = search;
        }

        [HttpPost]
        [Route("search")]
        public IHttpActionResult Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                var results = searchService.Search(searchRequest).ToList();

                var result = new SearchResultSet
                {
                    TotalResults = results.Count(),
                    SearchResults = results
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                throw new NovaHttpException(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}
