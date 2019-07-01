using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Search;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class Search
    {
        private readonly ISearchDispatcher searchDispatcher;

        public Search(ISearchDispatcher searchDispatcher)
        {
            this.searchDispatcher = searchDispatcher;
        }

        [FunctionName("InitiateSearch")]
        public async Task<string> InitiateSearch([HttpTrigger] HttpRequest request)
        {
            var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            return await searchDispatcher.DispatchSearch(searchRequest);
        }

        [FunctionName("RunSearch")]
        public async Task RunSearch(
            [ServiceBusTrigger("%MessagingServiceBus.SearchRequestsQueue%", Connection = "MessagingServiceBus.ConnectionString")]
            Message message)
        {
            var serialisedData = Encoding.UTF8.GetString(message.Body);
            var request = JsonConvert.DeserializeObject<IdentifiedSearchRequest>(serialisedData);

            await searchDispatcher.RunSearch(request);
        }
    }
}