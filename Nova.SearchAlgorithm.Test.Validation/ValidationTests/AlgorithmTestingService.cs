using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Api;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests
{
    public static class AlgorithmTestingService
    {
        private const string ApiKeyHeader = "X-Samples-ApiKey";
        private const string ApiKey = "test-key";

        private static TestServer server;

        public static void StartServer()
        {
            var builder = new WebHostBuilder()
                .UseContentRoot(Environment.CurrentDirectory)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddUserSecrets(Assembly.GetExecutingAssembly());
                })
                .UseStartup<Startup>();
            server = new TestServer(builder);
        }

        public static void StopServer()
        {
            server.Dispose();
        }

        public static async Task<SearchAlgorithmApiResult> Search(SearchRequest searchRequest)
        {
            var result = await server.CreateRequest("/search")
                .AddHeader(ApiKeyHeader, ApiKey)
                .And(request => request.Content = SerialiseToJson(searchRequest))
                .PostAsync();

            var content = await result.Content.ReadAsStringAsync();
            var deserializedContent = JsonConvert.DeserializeObject<SearchResultSet>(content);
            return new SearchAlgorithmApiResult
            {
                IsSuccess = result.IsSuccessStatusCode,
                StatusCode = result.StatusCode,
                ErrorMessage = result.IsSuccessStatusCode ? null : content,
                Results = result.IsSuccessStatusCode ? deserializedContent : null,
            };
        }

        public static async Task AddDonors(IEnumerable<InputDonor> donors)
        {
            var batch = new InputDonorBatch
            {
                Donors = donors
            };

            var result = await server.CreateRequest("/donor/batch")
                .AddHeader(ApiKeyHeader, ApiKey)
                .And(request => request.Content = SerialiseToJson(batch))
                .PostAsync();

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception("Request to add donors failed");
            }
        }

        public static async Task RunHlaRefresh()
        {
            var response = await server.CreateRequest("/trigger-donor-hla-update")
                .AddHeader(ApiKeyHeader, ApiKey)
                .PostAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Donor HLA Refresh Failed");
            }
        }

        private static StringContent SerialiseToJson(object obj)
        {
            return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
        }
    }
}