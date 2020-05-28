using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

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
        }

        private static string GetSqlConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}