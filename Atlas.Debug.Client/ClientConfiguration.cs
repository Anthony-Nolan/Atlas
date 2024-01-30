using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Registers all debug clients.
        /// Default request timeout is 5 minutes, if not stated in http function settings.
        /// </summary>
        public static void RegisterDebugClients(
            this IServiceCollection services,
            Func<IServiceProvider, DonorImportHttpFunctionSettings> fetchDonorImportHttpSettings)
        {
            services.RegisterDonorImportDebugClient(fetchDonorImportHttpSettings);
        }

        private static void RegisterDonorImportDebugClient(
            this IServiceCollection services,
            Func<IServiceProvider, DonorImportHttpFunctionSettings> fetchDonorImportHttpSettings)
        {
            services.AddHttpClient<IDonorImportClient, DonorImportClient>((sp, client) =>
            {
                var settings = fetchDonorImportHttpSettings(sp);
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = settings.RequestTimeOut ?? DefaultTimeout;
                client.DefaultRequestHeaders.Add(Accept, ApplicationJson);
            });

            services.AddSingleton<IDonorImportClient, DonorImportClient>(sp =>
                {
                    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var client = clientFactory.CreateClient(nameof(IDonorImportClient));
                    var settings = fetchDonorImportHttpSettings(sp);
                    return new DonorImportClient(client, settings.ApiKey);
                });
        }
    }
}