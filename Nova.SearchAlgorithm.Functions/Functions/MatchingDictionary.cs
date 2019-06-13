using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class MatchingDictionary
    {
        private readonly IMatchingDictionaryService matchingDictionaryService;

        public MatchingDictionary(IMatchingDictionaryService matchingDictionaryService)
        {
            this.matchingDictionaryService = matchingDictionaryService;
        }

        [FunctionName("RecreateMatchingDictionary")]
        public async Task Recreate([HttpTrigger] HttpRequest httpRequest)
        {
            await matchingDictionaryService.RecreateMatchingDictionary();
        }
    }
}