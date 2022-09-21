using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
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

            services.SetUpConfiguration();
            services.RegisterMatchPredictionAlgorithm(
                ApplicationInsightsSettingsReader,
                _ => new HlaMetadataDictionarySettings(),
                MacDictionarySettingsReader,
                MatchPredictionAlgorithmSettingsReader,
                _ => new NotificationsServiceBusSettings(),
                AzureStorageSettingsReader,
                ConnectionStringReader(MatchPredictionSqlConnectionString)
            );
            services.RegisterIntegrationTestServices();
            services.SetUpMockServices();

            // This call must be made after `RegisterMatchPredictionAlgorithm()`, as it overrides the non-mock dictionary set up in that method
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(ApplicationInsightsSettingsReader, _ => new MacDictionarySettings());
            services.SetUpMacDictionaryWithFileBackedRepository(
                ApplicationInsightsSettingsReader,
                MacDictionarySettingsReader);

            return services.BuildServiceProvider();
        }

        private static void SetUpConfiguration(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }

        private static void RegisterIntegrationTestServices(this IServiceCollection services)
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

        private static void SetUpMockServices(this IServiceCollection services)
        {
            services.AddScoped(sp => Substitute.For<INotificationSender>());
        }

        private static Func<IServiceProvider, ApplicationInsightsSettings> ApplicationInsightsSettingsReader =>
            _ => new ApplicationInsightsSettings { LogLevel = "Verbose" };

        private static Func<IServiceProvider, AzureStorageSettings> AzureStorageSettingsReader =>
            _ => new AzureStorageSettings { ConnectionString = "UseDevelopmentStorage=true", MatchPredictionResultsBlobContainer = "mpa-results" };

        private static Func<IServiceProvider, MacDictionarySettings> MacDictionarySettingsReader =>
            _ => new MacDictionarySettings();

        private static Func<IServiceProvider, MatchPredictionAlgorithmSettings> MatchPredictionAlgorithmSettingsReader =>
            _ => new MatchPredictionAlgorithmSettings();
    }
}