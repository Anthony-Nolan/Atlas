using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
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
        public async Task<ResultSet<MatchingAlgorithmResult>> Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                var results = (await searchService.Search(searchRequest, null)).ToList();

                return new OriginalMatchingAlgorithmResultSet
                {
                    TotalResults = results.Count,
                    Results = results
                };
            }
            catch (Exception e)
            {
                throw new SearchHttpException("There was a problem while executing a search", e);
            }
        }
    }
}