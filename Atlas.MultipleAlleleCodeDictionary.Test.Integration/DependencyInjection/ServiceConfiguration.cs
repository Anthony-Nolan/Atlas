using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.RegisterSettings();
            services.SetUpConfiguration();
            services.RegisterMacDictionary(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>()
            );
            services.SetUpIntegrationTestServices();
            services.SetUpMockServices();
            return services.BuildServiceProvider();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<MacDictionarySettings>("MacDictionary");
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
            services.AddScoped<ITestMacRepository, TestMacRepository>();
        }

        private static void SetUpMockServices(this IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IMacCodeDownloader>());
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }
    }
}