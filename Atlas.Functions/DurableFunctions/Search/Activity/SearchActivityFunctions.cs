using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Activity
{
    public class SearchActivityFunctions
    {
        private readonly ISearchRunner searchRunner;

        public SearchActivityFunctions(ISearchRunner searchRunner)
        {
            this.searchRunner = searchRunner;
        }
        
        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<SearchResultSet> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            return await searchRunner.RunSearch(searchRequest);
        }
    }
}