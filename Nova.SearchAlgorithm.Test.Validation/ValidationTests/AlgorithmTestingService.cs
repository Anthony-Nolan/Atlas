using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests
{
    public static class AlgorithmTestingService
    {
        private const string ApiKeyHeader = "X-Samples-ApiKey";
        private const string ApiKey = "test-key";

        private static TestServer server;

        public static void StartServer()
        {
            server = TestServer.Create<Startup>();
        }

        public static void StopServer()
        {
            server.Dispose();
        }

        public static async Task<SearchResultSet> Search(SearchRequest searchRequest)
        {
            var result = await server.CreateRequest("/search")
                .AddHeader(ApiKeyHeader, ApiKey)
                .And(request => request.Content = SerialiseToJson(searchRequest))
                .PostAsync();

            var content = await result.Content.ReadAsStringAsync();
            var deserialisedContent = JsonConvert.DeserializeObject<SearchResultSet>(content);
            return deserialisedContent;
        }

        public static void RunHlaRefresh()
        {
            Task.Run(() => server.CreateRequest("/trigger-donor-hla-update")
                .AddHeader(ApiKeyHeader, ApiKey)
                .PostAsync()).Wait();
        }

        private static StringContent SerialiseToJson(SearchRequest searchRequest)
        {
            return new StringContent(JsonConvert.SerializeObject(searchRequest), Encoding.UTF8, "application/json");
        }
    }
}