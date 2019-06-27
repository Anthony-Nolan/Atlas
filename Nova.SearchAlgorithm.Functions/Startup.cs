using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.DependencyInjection;
using Nova.SearchAlgorithm.Functions;
using Nova.SearchAlgorithm.Settings;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Nova.SearchAlgorithm.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder);
            builder.Services.RegisterClients();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterMatchingDictionaryTypes();
            builder.Services.RegisterSearchAlgorithmTypes();
        }

        private void RegisterSettings(IFunctionsHostBuilder builder)
        {
            AddUserSecrets(builder);
            RegisterSettings<ApplicationInsightsSettings>(builder);
            RegisterSettings<AzureStorageSettings>(builder);
            RegisterSettings<DonorServiceSettings>(builder);
            RegisterSettings<HlaServiceSettings>(builder);
            RegisterSettings<WmdaSettings>(builder);
        }

        private static void AddUserSecrets(IFunctionsHostBuilder functionsHostBuilder)
        {
            var configurationBuilder = new ConfigurationBuilder();
            // Fetch the existing IConfiguration set up by the azure functions framework
            var descriptor = functionsHostBuilder.Services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
            if (descriptor?.ImplementationInstance is IConfigurationRoot configuration)
            {
                // IMPORTANT: We must re-add the old configuration to ensure we do not lose any settings e.g. those from host.json, which are set up by the framework
                configurationBuilder.AddConfiguration(configuration);
            }

            configurationBuilder.AddUserSecrets("710bde86-9075-4086-9657-ad605368265f");
            functionsHostBuilder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), configurationBuilder.Build()));
        }

        /// <summary>
        /// The common search algorithm project relies on the same app settings regardless of whether it is called by this function, or the web api.
        /// Both frameworks use different configuration patterns:
        /// - ASP.NET Core uses the Options pattern with nested settings in appsettings.json
        /// - Functions v2 uses a flat collections of string app settings in the "Values" object of local.settings.json
        ///
        /// This method explicitly sets up the IOptions classes that would be set up by "services.Configure".
        /// All further DI can assume these IOptions are present in either scenario
        /// </summary>
        private static void RegisterSettings<TSettings>(IFunctionsHostBuilder builder) where TSettings : class, new()
        {
            builder.Services.AddSingleton<IOptions<TSettings>>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                return new OptionsWrapper<TSettings>(BuildSettings<TSettings>(config));
            });
        }

        private static TSettings BuildSettings<TSettings>(IConfiguration config) where TSettings : class, new()
        {
            var settings = new TSettings();

            switch (settings)
            {
                case ApplicationInsightsSettings applicationInsightsSettings:
                    applicationInsightsSettings.InstrumentationKey = config.GetSection("ApplicationInsights.InstrumentationKey").Value;
                    applicationInsightsSettings.LogLevel = config.GetSection("ApplicationInsights.LogLevel").Value;
                    break;
                case AzureStorageSettings azureStorageSettings:
                    azureStorageSettings.ConnectionString = config.GetSection("AzureStorage.ConnectionString").Value;
                    break;
                case DonorServiceSettings donorServiceSettings:
                    donorServiceSettings.ApiKey = config.GetSection("Client.DonorService.ApiKey").Value;
                    donorServiceSettings.BaseUrl = config.GetSection("Client.DonorService.BaseUrl").Value;
                    break;
                case HlaServiceSettings hlaServiceSettings:
                    hlaServiceSettings.ApiKey = config.GetSection("Client.HlaService.ApiKey").Value;
                    hlaServiceSettings.BaseUrl = config.GetSection("Client.HlaService.BaseUrl").Value;
                    break;
                case WmdaSettings wmdaSettings:
                    wmdaSettings.HlaDatabaseVersion = config.GetSection("Wmda.HlaDatabaseVersion").Value;
                    wmdaSettings.WmdaFileUri = config.GetSection("Wmda.FileUri").Value;
                    break;
            }

            return settings;
        }
    }
}