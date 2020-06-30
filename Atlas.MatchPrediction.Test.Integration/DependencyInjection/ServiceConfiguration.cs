using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.DependencyInjection;
using Atlas.MatchPrediction.Settings.Azure;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Test.Integration.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        private const string MatchPredictionSqlConnectionString = "MatchPredictionSql";

        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            SetUpConfiguration(services);
            services.RegisterMatchPredictionServices(
                ApplicationInsightsSettingsReader,
                _ => new AzureStorageSettings(),
                _ => new HlaMetadataDictionarySettings(),
                _ => new MacDictionarySettings(),
                _ => new NotificationsServiceBusSettings(),
                ConnectionStringReader(MatchPredictionSqlConnectionString)
            );
            RegisterIntegrationTestServices(services);
            SetUpMockServices(services);
            
            // This call must be made after `RegisterMatchPredictionServices()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(ApplicationInsightsSettingsReader, _ => new MacDictionarySettings());
            services.SetUpMacDictionaryWithFileBackedRepository(
                _ => new ApplicationInsightsSettings{ LogLevel = "Info"}, 
                _ => new MacDictionarySettings());

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
                var connectionString = ConnectionStringReader(MatchPredictionSqlConnectionString)(sp);
                return new ContextFactory().Create(connectionString);
            });

            services.AddScoped<IHaplotypeFrequencyInspectionRepository>(sp =>
                new HaplotypeFrequencyInspectionRepository(ConnectionStringReader(MatchPredictionSqlConnectionString)(sp))
            );
        }

        private static void SetUpMockServices(IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }

        private static Func<IServiceProvider, ApplicationInsightsSettings> ApplicationInsightsSettingsReader =>
            _ => new ApplicationInsightsSettings {LogLevel = "Info"};
    }
}