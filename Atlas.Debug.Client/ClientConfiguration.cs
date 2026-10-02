using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;

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

        /// <summary>
        /// Registers debug clients from configuration sections.
        /// Each tuple maps a configuration section name to an interface/implementation pair.
        /// Settings are validated at registration time — missing or placeholder values cause an immediate exception.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root containing the client settings sections.</param>
        /// <param name="clients">
        /// Tuples of (Section name, Interface type, Client implementation type) where the section
        /// must contain <c>BaseUrl</c> and <c>ApiKey</c> values.
        /// </param>
        public static void RegisterDebugClients(
            this IServiceCollection services,
            IConfigurationRoot configuration,
            params (string Section, Type Interface, Type Client)[] clients)
        {
            foreach (var (section, iface, client) in clients)
            {
                var settings = GetSettingsOrThrow(configuration, section);

                services.AddHttpClient(section, httpClient =>
                {
                    httpClient.BaseAddress = new Uri(settings.BaseUrl);
                    httpClient.Timeout = settings.RequestTimeOut ?? DefaultTimeout;
                    httpClient.DefaultRequestHeaders.Add(Accept, ApplicationJson);
                    httpClient.DefaultRequestHeaders.Add(ApiKeyName, settings.ApiKey);
                });

                services.AddTransient(iface, sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    return Activator.CreateInstance(client, factory.CreateClient(section))!;
                });
            }
        }

        private static HttpFunctionSettings GetSettingsOrThrow(IConfigurationRoot configuration, string sectionName)
        {
            var settings = configuration.GetSection(sectionName).Get<ConcreteHttpFunctionSettings>();

            if (settings == null)
            {
                throw new InvalidOperationException(
                    $"Configuration section '{sectionName}' is missing. " +
                    $"Add it to appsettings.json or user secrets with 'BaseUrl' and 'ApiKey' values.");
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.BaseUrl)
                || string.Equals(settings.BaseUrl, "override-this", StringComparison.OrdinalIgnoreCase)
                || !Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _))
                errors.Add($"'{sectionName}:BaseUrl' is not configured or not a valid URL (current value: '{settings.BaseUrl ?? "null"}')");

            if (string.IsNullOrWhiteSpace(settings.ApiKey)
                || string.Equals(settings.ApiKey, "override-this", StringComparison.OrdinalIgnoreCase))
                errors.Add($"'{sectionName}:ApiKey' is not configured (current value: '{settings.ApiKey ?? "null"}')");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Configuration errors in '{sectionName}':\n  - {string.Join("\n  - ", errors)}\n" +
                    $"Set the correct values in user secrets (dotnet user-secrets set \"{sectionName}:BaseUrl\" \"<url>\").");
            }

            return settings;
        }

        private class ConcreteHttpFunctionSettings : HttpFunctionSettings;

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