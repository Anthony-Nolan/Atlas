using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.TestHelpers;
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

            // Off, as in the hosts until a Redis is configured. ActiveSetDistributedCacheTests shares one between two caches.
            services.RegisterDistributedCacheTypes(_ => new DistributedCacheSettings());

            services.RegisterMatchPredictionAlgorithm(
                ApplicationInsightsSettingsReader,
                _ => new HlaMetadataDictionarySettings(),
                MacDictionarySettingsReader,
                _ => new NotificationsServiceBusSettings(),
                AzureStorageSettingsReader,
                _ => new GenotypeImputationSettings(),
                _ => new PrecomputedGenotypeSetSettings(),
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
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
            services.RegisterAsOptions<HaplotypeFrequencySetCacheSettings>("HaplotypeFrequencySetCache");
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

            // Each import writes the active sets to the distributed cache a second time, 30 seconds later. Many tests import
            // sets, and none of them tests that delay.
            services.AddSingleton<TimeProvider>(new ImmediateTimersTimeProvider());
        }

        private static Func<IServiceProvider, ApplicationInsightsSettings> ApplicationInsightsSettingsReader =>
            _ => new ApplicationInsightsSettings { LogLevel = "Verbose" };

        private static Func<IServiceProvider, AzureStorageSettings> AzureStorageSettingsReader =>
            _ => new AzureStorageSettings { ConnectionString = "UseDevelopmentStorage=true", MatchPredictionResultsBlobContainer = "mpa-results" };

        private static Func<IServiceProvider, MacDictionarySettings> MacDictionarySettingsReader =>
            _ => new MacDictionarySettings();
    }
}