using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Search;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class Search
    {
        private readonly ISearchService searchService;
        private readonly ISearchDispatcher searchDispatcher;

        public Search(ISearchService searchService, ISearchDispatcher searchDispatcher)
        {
            this.searchService = searchService;
            this.searchDispatcher = searchDispatcher;
        }

        [FunctionName("InitiateSearch")]
        public async Task<string> InitiateSearch([HttpTrigger] HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            return await searchDispatcher.DispatchSearch(searchRequest);
        }
    }
}