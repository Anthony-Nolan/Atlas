using Atlas.Common.Notifications;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.DependencyInjection;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.IO;
using Atlas.MultipleAlleleCodeDictionary.HlaService;

namespace Atlas.MatchPrediction.Test.Integration.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            SetUpConfiguration(services);
            services.RegisterMatchPredictionServices();
            RegisterIntegrationTestServices(services);
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

        private static void RegisterIntegrationTestServices(IServiceCollection services)
        {
            services.AddScoped(sp =>
            {
                var connectionString = GetSqlConnectionString(sp);
                return new ContextFactory().Create(connectionString);
            });

            services.AddScoped<IHaplotypeFrequencyInspectionRepository>(sp =>
              new HaplotypeFrequencyInspectionRepository(GetSqlConnectionString(sp))
            );
        }

        private static void SetUpMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<INotificationsClient>());
            services.AddScoped(sp => Substitute.For<IHlaServiceClient>());
        }

        private static string GetSqlConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}