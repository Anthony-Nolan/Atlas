using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchingAlgorithm.Api;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.SearchTracking.Common.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests
{
    public static class AlgorithmTestingService
    {
        private const string ApiKeyHeader = "X-Samples-ApiKey";
        private const string ApiKey = "test-key";

        private static IHost host;
        private static TestServer server;

        public static void StartServer()
        {
            host = new HostBuilder()
                .UseContentRoot(Environment.CurrentDirectory)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddUserSecrets(Assembly.GetExecutingAssembly());

                    AddFeatureManagement(config);
                })
                .ConfigureWebHost(webHost => webHost
                    .UseTestServer()
                    .UseStartup<Startup>()
                    .ConfigureTestServices(RegisterTestServices))
                .Start();
            server = host.GetTestServer();
        }

        private static void RegisterTestServices(IServiceCollection services)
        {
            // The test data is at the file-backed HLA nomenclature version, so the dictionaries are read from the files
            // that the integration tests use, rather than from a storage account prepared in advance.
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>());

            // Every search publishes search tracking events, and failures raise notifications. These tests check matching and
            // scoring only, so nothing reads those messages: sending them to a real Service Bus would only slow each search down.
            services.AddScoped(_ => Substitute.For<ISearchTrackingServiceBusClient>());
            services.AddScoped(_ => Substitute.For<INotificationsClient>());
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
            host.Dispose();
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