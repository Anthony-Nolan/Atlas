using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Context;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atlas.DonorImport.Test.Integration.DependencyInjection
{
    public class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            SetUpConfiguration(services);
            services.RegisterDonorImport();
            RegisterIntegrationTestServices(services);
            SetUpMockServices(services);
            return services.BuildServiceProvider();
        }

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                var connectionString = GetSqlConnectionString(sp);
                return new ContextFactory().Create(connectionString);
            });
            
            services.AddScoped<IDonorInspectionRepository>(sp =>
                new DonorInspectionRepository(GetSqlConnectionString(sp))
            );
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

            services.AddScoped(sp => mockSearchServiceBusClient);
            services.AddScoped(sp => Substitute.For<INotificationsClient>());
        }

        private static string GetSqlConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}