using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection
{
    public static class ServiceConfiguration
    {
        private const string PersistentSqlConnectionStringKey = "PersistentSql";
        private const string TransientASqlConnectionStringKey = "SqlA";
        private const string TransientBSqlConnectionStringKey = "SqlB";
        private const string DonorImportSqlConnectionStringKey = "DonorImportSql";

        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);

            services.RegisterSettings();
            services.RegisterMatchingAlgorithm(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                _ => new AzureAuthenticationSettings(),
                _ => new AzureAppServiceManagementSettings(),
                _ => new AzureDatabaseManagementSettings(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<DataRefreshSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                _ => new MacDictionarySettings(),
                _ => new MessagingServiceBusSettings(),
                _ => new NotificationsServiceBusSettings(),
                ConnectionStringReader(PersistentSqlConnectionStringKey),
                ConnectionStringReader(TransientASqlConnectionStringKey),
                ConnectionStringReader(TransientBSqlConnectionStringKey),
                ConnectionStringReader(DonorImportSqlConnectionStringKey)
            );
            services.RegisterMatchingAlgorithmDonorManagement(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                _ => new DonorManagementSettings(), 
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                _ => new MacDictionarySettings(),
                _ => new MessagingServiceBusSettings(),
                _ => new NotificationsServiceBusSettings(),
                ConnectionStringReader(PersistentSqlConnectionStringKey),
                ConnectionStringReader(TransientASqlConnectionStringKey),
                ConnectionStringReader(TransientBSqlConnectionStringKey)
            );

            // This call must be made after `RegisterMatchingAlgorithm()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                //These configuration values won't be used, because all they are all (indirectly) overridden, below.
                OptionsReaderFor<ApplicationInsightsSettings>()
            );

            services.AddScoped(sp =>
                new ContextFactory().Create(ConnectionStringReader(TransientASqlConnectionStringKey)(sp))
            );

            RegisterMockServices(services);
            RegisterIntegrationTestServices(services);

            return services.BuildServiceProvider();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<AzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DataRefreshSettings>("DataRefresh");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
        }

        private static void RegisterMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<IDonorReader>());

            // Clients
            var mockSearchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            mockSearchServiceBusClient
                .PublishToSearchQueue(Arg.Any<IdentifiedSearchRequest>())
                .Returns(Task.CompletedTask);
            services.AddScoped(sp => mockSearchServiceBusClient);

            services.AddScoped(sp => Substitute.For<INotificationSender>());

            services.AddScoped(sp => Substitute.For<IAzureDatabaseManager>());
            services.AddScoped(sp => Substitute.For<IAzureFunctionManager>());
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped<ITestDataRefreshHistoryRepository, TestDataRefreshHistoryRepository>();
        }
    }
}