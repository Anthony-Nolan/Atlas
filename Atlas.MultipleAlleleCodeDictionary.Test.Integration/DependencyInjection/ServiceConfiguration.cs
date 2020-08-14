using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection
{
    public static class ServiceConfiguration
    {
        internal static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.RegisterSettings();
            services.SetUpConfiguration();
            services.RegisterMacDictionary(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
            );
            services.RegisterMacImport(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MacDownloadSettings>()
            );
            services.SetUpIntegrationTestServices();
            services.SetUpMockServices();
            return services.BuildServiceProvider();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MacDownloadSettings>("MacDictionary:Download");
        }

        private static void SetUpConfiguration(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }

        private static void SetUpIntegrationTestServices(this IServiceCollection services)
        {
        }

        private static void SetUpMockServices(this IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IMacCodeDownloader>());
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }

        public static void SetUpMacDictionaryWithFileBackedRepository(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings)
        {
            services.RegisterMacDictionary(
                fetchApplicationInsightsSettings,
                fetchMacDictionarySettings
            );

            services.RegisterLifeTimeScopedCacheTypes();
            services.AddSingleton<IMacRepository, FileBackedMacDictionaryRepository>();
        }
    }
}