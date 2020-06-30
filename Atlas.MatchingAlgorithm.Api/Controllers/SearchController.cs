using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.Search;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
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
        public async Task<MatchingAlgorithmResultSet> Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                var results = (await searchService.Search(searchRequest)).ToList();

                return new MatchingAlgorithmResultSet
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