using System;
using Atlas.Functions.PublicApi.Settings;
using Atlas.MatchingAlgorithm.Clients.Scoring;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Functions.PublicApi.ClientConfig
{
    public static class ClientConfiguration
    {
        private const string Accept = "Accept";
        private const string ApplicationJson = "application/json";
        private const string ApiKeyName = "x-functions-key";

        public static void RegisterClients(
            this IServiceCollection services,
            Func<IServiceProvider, MatchingAlgorithmFunctionSettings> fetchMatchingAlgorithmSettings
        )
        {
            services.RegisterHttpFunctionClient<IMatchingAlgorithmScoringFunctionsClient, MatchingAlgorithmScoringFunctionsClient>(
                fetchMatchingAlgorithmSettings);
        }

        private static void RegisterHttpFunctionClient<TInterface, TClient>(
            this IServiceCollection services,
            Func<IServiceProvider, MatchingAlgorithmFunctionSettings> fetchHttpSettings)
            where TInterface : class
            where TClient : MatchingAlgorithmHttpFunctionClient, TInterface
        {
            // Using .NET Core's built-in HttpClientFactory to create the typed client.
            // The factory automatically takes care of HttpClient management in a way that prevents socket exceptions.
            services.AddHttpClient<TInterface, TClient>((sp, client) =>
            {
                var settings = fetchHttpSettings(sp);
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Add(Accept, ApplicationJson);
                client.DefaultRequestHeaders.Add(ApiKeyName, settings.ApiKey);
            });
        }
    }
}
