using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.DependencyInjection;
using Atlas.MatchPrediction.Settings.Azure;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atlas.MatchPrediction.Test.Integration.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            SetUpConfiguration(services);
            services.RegisterMatchPredictionServices(
                _ => new ApplicationInsightsSettings {LogLevel = "Info"},
                _ => new AzureStorageSettings(),
                _ => new HlaMetadataDictionarySettings(),
                _ => new MacDictionarySettings(),
                _ => new NotificationsServiceBusSettings()
            );
            RegisterIntegrationTestServices(services);
            SetUpMockServices(services);

            // This call must be made after `RegisterMatchPredictionServices()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                //These configuration values won't be used, because all they are all (indirectly) overridden, below.
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>()
            );

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
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }

        private static string GetSqlConnectionString(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IConfiguration>().GetSection("ConnectionStrings")["Sql"];
        }
    }
}