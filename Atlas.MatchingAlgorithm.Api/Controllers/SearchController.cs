using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
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
                var results = (await searchService.Search(searchRequest, null)).ToList();

                return new MatchingAlgorithmResultSet
                {
                    ResultCount = results.Count(),
                    MatchingAlgorithmResults = results
                };
            }
            catch (Exception e)
            {
                throw new SearchHttpException("There was a problem while executing a search", e);
            }
        }
    }
}