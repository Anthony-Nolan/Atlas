using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Context;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
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
            var services = new ServiceCollection();
            
            SetUpConfiguration(services);
            services.RegisterDonorImport(
                sp => new ApplicationInsightsSettings {LogLevel = "Info"},
                sp => new MessagingServiceBusSettings(),
                sp => new NotificationsServiceBusSettings(),
                ConnectionStringReader(DonorStoreSqlConnectionString)
            );
            RegisterIntegrationTestServices(services);
            SetUpMockServices(services);
            return services.BuildServiceProvider();
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped(sp => new ContextFactory().Create(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));
            services.AddScoped<IDonorInspectionRepository>(sp =>
                new DonorInspectionRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));
            services.AddScoped<IDonorImportHistoryRepository>(sp => new DonorImportHistoryRepository(ConnectionStringReader(DonorStoreSqlConnectionString)(sp)));
            services.AddScoped<IDonorImportFileHistoryService, DonorImportFileHistoryService>();
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
            var mockSearchServiceBusClient = Substitute.For<IMessagingServiceBusClient>();
            mockSearchServiceBusClient
                .PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>())
                .Returns(Task.CompletedTask);
            
            services.AddScoped(sp => Substitute.For<ILogger>());
            services.AddScoped(sp => mockSearchServiceBusClient);
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }
    }
}