using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Atlas.Debug.Client
{
    /// <summary>
    /// Methods for registration of debug clients.
    /// </summary>  
    public static class ClientConfiguration
    {
        private const string Accept = "Accept";
        private const string ApplicationJson = "application/json";
        private const string ApiKeyName = "x-functions-key";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Registers all debug clients.
        /// Default request timeout is 5 minutes, if not stated in http function settings.
        /// </summary>
        public static void RegisterDebugClients(
            this IServiceCollection services,
            Func<IServiceProvider, DonorImportHttpFunctionSettings> fetchDonorImportHttpSettings,
            Func<IServiceProvider, MatchingAlgorithmHttpFunctionSettings> fetchMatchingAlgorithmHttpSettings,
            Func<IServiceProvider, TopLevelHttpFunctionSettings> fetchTopLevelHttpSettings,
            Func<IServiceProvider, PublicApiHttpFunctionSettings> fetchPublicApiHttpSettings,
            Func<IServiceProvider, RepeatSearchHttpFunctionSettings> fetchRepeatSearchHttpSettings
            )
        {
            services.RegisterHttpFunctionClient<IDonorImportFunctionsClient, DonorImportFunctionsClient>(fetchDonorImportHttpSettings);
            services.RegisterHttpFunctionClient<IMatchingAlgorithmFunctionsClient, MatchingAlgorithmFunctionsClient>(fetchMatchingAlgorithmHttpSettings);
            services.RegisterHttpFunctionClient<ITopLevelFunctionsClient, TopLevelFunctionsClient>(fetchTopLevelHttpSettings);
            services.RegisterHttpFunctionClient<IPublicApiFunctionsClient, PublicApiFunctionsClient>(fetchPublicApiHttpSettings);
            services.RegisterHttpFunctionClient<IRepeatSearchFunctionsClient, RepeatSearchFunctionsClient>(fetchRepeatSearchHttpSettings);
        }

        private static void RegisterHttpFunctionClient<TInterface, TClient>(
            this IServiceCollection services,
            Func<IServiceProvider, HttpFunctionSettings> fetchHttpFunctionSettings)
                where TInterface : class
                where TClient : HttpFunctionClient, TInterface
        {
            // Using .NET Core's built-in HttpClientFactory to create the typed client.
            // The factory automatically takes care of HttpClient management in a way that prevents socket exceptions.
            services.AddHttpClient<TInterface, TClient>((sp, client) =>
            {
                var settings = fetchHttpFunctionSettings(sp);
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = settings.RequestTimeOut ?? DefaultTimeout;
                client.DefaultRequestHeaders.Add(Accept, ApplicationJson);
                client.DefaultRequestHeaders.Add(ApiKeyName, settings.ApiKey);
            });
        }
    }
}