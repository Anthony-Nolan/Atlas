using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class Search
    {
        private readonly ISearchService searchService;

        public Search(ISearchService searchService)
        {
            this.searchService = searchService;
        }

        [FunctionName("Search")]
        public async Task<IEnumerable<SearchResult>> RunSearch([HttpTrigger] HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            return await searchService.Search(searchRequest);
        }
    }
}