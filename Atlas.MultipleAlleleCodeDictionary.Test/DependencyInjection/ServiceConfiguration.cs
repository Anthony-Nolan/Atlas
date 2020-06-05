using System;
using System.IO;
using Atlas.Common.Notifications;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atlas.MultipleAlleleCodeDictionary.Test.DependencyInjection
{
    internal class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            SetUpConfiguration(services);
            services.RegisterMacDictionaryImportTypes();
            /*SetUpIntegrationTestServices(services); */
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

        private static void SetUpMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IMacCodeDownloader>());
            services.AddScoped(sp => Substitute.For<INotificationsClient>());
            services.AddScoped(sp => Substitute.For<CloudTable>());
            services.AddScoped(sp => Substitute.For<IMacRepository>());
        }
    }
}