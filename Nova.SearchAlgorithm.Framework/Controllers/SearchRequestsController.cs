using System;
using System.Linq;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Exceptions;

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
        public async Task<SearchResultSet> Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                var results = (await searchService.Search(searchRequest)).ToList();

                var result = new SearchResultSet
                {
                    TotalResults = results.Count(),
                    SearchResults = results
                };

                return result;
            }
            catch (Exception e)
            {
                throw new SearchHttpException("There was a problem while executing a search", e);
            }
        }
    }
}
