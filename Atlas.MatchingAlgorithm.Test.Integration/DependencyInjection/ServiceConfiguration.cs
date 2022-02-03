using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
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
    internal static class ServiceConfiguration
    {
        internal static IDonorReader MockDonorReader;
        
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
            services.RegisterSearch(OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                _ => new MacDictionarySettings(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                _ => new NotificationsServiceBusSettings(),
                _ => new MatchingConfigurationSettings {MatchingBatchSize = 250000},
                ConnectionStringReader(PersistentSqlConnectionStringKey),
                ConnectionStringReader(TransientASqlConnectionStringKey),
                ConnectionStringReader(TransientBSqlConnectionStringKey),
                ConnectionStringReader(DonorImportSqlConnectionStringKey));

            services.RegisterDataRefresh(
                _ => new AzureAuthenticationSettings(),
                _ => new AzureDatabaseManagementSettings(),
                OptionsReaderFor<DataRefreshSettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                _ => new MacDictionarySettings(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                _ => new NotificationsServiceBusSettings(),
                OptionsReaderFor<DonorManagementSettings>(),
                ConnectionStringReader(PersistentSqlConnectionStringKey),
                ConnectionStringReader(TransientASqlConnectionStringKey),
                ConnectionStringReader(TransientBSqlConnectionStringKey),
                ConnectionStringReader(DonorImportSqlConnectionStringKey));

            services.RegisterDonorManagement(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<AzureStorageSettings>(),
                OptionsReaderFor<DonorManagementSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                _ => new MacDictionarySettings(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                _ => new NotificationsServiceBusSettings(),
                ConnectionStringReader(PersistentSqlConnectionStringKey),
                ConnectionStringReader(TransientASqlConnectionStringKey),
                ConnectionStringReader(TransientBSqlConnectionStringKey));

            // This call must be made after `RegisterMatchingAlgorithm()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                //These configuration values won't be used, because all they are all (indirectly) overridden, below.
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
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
            services.RegisterAsOptions<DonorManagementSettings>("DonorManagement");
            services.RegisterAsOptions<MessagingServiceBusSettings>("DonorManagementMessagingServiceBus");
        }

        private static void RegisterMockServices(IServiceCollection services)
        {
            // Clients
            var mockSearchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            mockSearchServiceBusClient
                .PublishToSearchRequestsTopic(Arg.Any<IdentifiedSearchRequest>())
                .Returns(Task.CompletedTask);
            services.AddScoped(sp => mockSearchServiceBusClient);

            services.AddScoped(sp => Substitute.For<IMessageReceiverFactory>());
            services.AddScoped(sp => Substitute.For<INotificationSender>());
            services.AddScoped(sp => Substitute.For<IAzureDatabaseManager>());

            MockDonorReader = Substitute.For<IDonorReader>();
            MockDonorReader.GetDonors(default).ReturnsForAnyArgs(callInfo =>
            {
                var ids = callInfo.Arg<IEnumerable<int>>();
                return ids.ToDictionary(id => id, id => new Donor {AtlasDonorId = id, ExternalDonorCode = id.ToString()});
            });
            services.AddScoped(_ => MockDonorReader);

            // Log to file, not to ApplicationInsights!
            services.AddSingleton<ILogger, FileBasedLogger>();
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.RegisterLifeTimeScopedCacheTypes();
            services.AddScoped<ITestDataRefreshHistoryRepository, TestDataRefreshHistoryRepository>();
        }
    }
}