using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Services.Search;

namespace Nova.SearchAlgorithm.Api.Controllers
{
    public class SearchController : ControllerBase
    {
        private readonly ISearchService searchService;

        public SearchController(ISearchService searchService)
        {
            this.searchService = searchService;
        }
    
        [HttpPost]
        [Route("search")]
        public async Task<SearchResultSet> Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                var results = (await searchService.Search(searchRequest)).ToList();

                return new SearchResultSet
                {
                    TotalResults = results.Count(),
                    SearchResults = results
                };
            }
            catch (Exception e)
            {
                throw new SearchHttpException("There was a problem while executing a search", e);
            }
        }
    }
}