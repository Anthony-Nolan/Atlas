using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.MatchingAlgorithm.Api;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests
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

                    AddFeatureManagement(config);
                })
                .UseStartup<Startup>();
            server = new TestServer(builder);
        }

        /// <summary>
        /// Feature management, leave it configured even if there is no active feature flags in use
        /// </summary>
        /// <param name="configBuilder">Configuration builder</param>
        private static void AddFeatureManagement(IConfigurationBuilder configBuilder)
        {
            var configuration = configBuilder.Build();
            var azureConfigurationConnectionString = configuration.GetValue<string>("AzureAppConfiguration:ConnectionString");
            configBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(azureConfigurationConnectionString)
                    .Select("_")
                    .UseFeatureFlags();
            });
        }

        public static void StopServer()
        {
            server.Dispose();
        }

        public static async Task<SearchAlgorithmApiResult> Search(SearchRequest matchingRequest)
        {
            var result = await server.CreateRequest("/search")
                .AddHeader(ApiKeyHeader, ApiKey)
                .And(request => request.Content = SerialiseToJson(matchingRequest))
                .PostAsync();

            var content = await result.Content.ReadAsStringAsync();
            var deserializedContent = JsonConvert.DeserializeObject<OriginalMatchingAlgorithmResultSet>(content);
            return new SearchAlgorithmApiResult
            {
                IsSuccess = result.IsSuccessStatusCode,
                StatusCode = result.StatusCode,
                ErrorMessage = result.IsSuccessStatusCode ? null : content,
                Results = result.IsSuccessStatusCode ? deserializedContent : null,
            };
        }

        public static async Task AddDonors(IEnumerable<DonorInfo> donors)
        {
            var batch = new DonorInfoBatch
            {
                Donors = donors
            };

            var result = await server.CreateRequest("/donor/batch")
                .AddHeader(ApiKeyHeader, ApiKey)
                .And(request => request.Content = SerialiseToJson(batch))
                .SendAsync("PUT");

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