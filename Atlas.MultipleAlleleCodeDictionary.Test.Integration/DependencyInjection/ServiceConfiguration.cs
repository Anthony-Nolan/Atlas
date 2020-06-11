using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection
{
    internal class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<MacImportSettings>("MacImport");
            SetUpConfiguration(services);
            services.RegisterMacDictionary(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacImportSettings>()
                );
            services.RegisterMacDictionaryUsageServices(
                sp => "TODO: Remove HLA Service",
                sp => "TODO: Remove HLA Service",
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value
            );
            SetUpIntegrationTestServices(services);
            SetUpMockServices(services);
            return services.BuildServiceProvider();
        }

        private static void SetUpConfiguration(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }
        
        private static void SetUpIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped<ITestMacRepository, TestMacRepository>();
        }
        
        private static void SetUpMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IMacCodeDownloader>());
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }
    }
}