using System;
using System.IO;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.Data.Context;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.DonorImport.Test.Integration.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        private const string DonorStoreSqlConnectionString = "DonorStoreSql";

        public static IServiceProvider CreateProvider()
        {
            var services = BuildServiceCollection();
            return services.BuildServiceProvider();
        }

        // Expose service collection to allow individual test suites to mock classes if needed
        public static ServiceCollection BuildServiceCollection()
        {
            var services = new ServiceCollection();

            SetUpConfiguration(services);
            services.RegisterDonorImport(
                sp => new ApplicationInsightsSettings { LogLevel = "Info" },
                sp => new MessagingServiceBusSettings(),
                sp => new NotificationConfigurationSettings(),
                sp => new NotificationsServiceBusSettings(),
                sp => new DonorImportSettings { HoursToCheckStalledFiles = 2, AllowFullModeImport = true },
                sp => new PublishDonorUpdatesSettings(),
                sp => new AzureStorageSettings(),
                sp => new FailureLogsSettings { ExpiryInDays = 60 },
                ConnectionStringReader(DonorStoreSqlConnectionString)
            );
            RegisterIntegrationTestServices(services);
            SetUpMockServices(services);

            return services;
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped(sp => new ContextFactory().Create(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));

            services.AddScoped<IDonorInspectionRepository>(sp =>
                new DonorInspectionRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));

            services.AddScoped<IDonorImportHistoryRepository>(sp =>
                new DonorImportHistoryRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));
            services.AddScoped<IDonorImportFileHistoryService, DonorImportFileHistoryService>();

            services.AddScoped<IPublishableDonorUpdatesInspectionRepository>(sp =>
                new PublishableDonorUpdatesInspectionRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));

            services.AddScoped<IDonorImportFailuresInspectionRepository>(sp => 
                new DonorImportFailuresInspectionRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));
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
            #region Mock up topic client for Notifications sender
            // Service bus client package will throw if it detects an ongoing transaction, as it doesn't support distributed transactions.
            // We emulate that on all service bus clients here to enable testing for such cases.
            var mockTopicClient = Substitute.For<ITopicClient>();
            mockTopicClient
                .WhenForAnyArgs(x => x.SendAsync((ServiceBusMessage)default))
                .Do(_ => ThrowIfInTransaction());

            var mockTopicClientFactory = Substitute.For<ITopicClientFactory>();
            mockTopicClientFactory.BuildTopicClient(default).ReturnsForAnyArgs(mockTopicClient);
            services.AddScoped(sp => mockTopicClientFactory);
            #endregion

            services.AddScoped(sp => Substitute.For<ILogger>());
            services.AddScoped(sp => Substitute.For<IMessageBatchPublisher<SearchableDonorUpdate>>());
        }

        private static void ThrowIfInTransaction()
        {
            if (Transaction.Current != null)
            {
                throw new Exception("Transaction detected. Throwing as we do not want to support transactions in this context...");
            }
        }
    }
}