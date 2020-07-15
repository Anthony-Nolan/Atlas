using System;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static IServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                _ => new ApplicationInsightsSettings(){LogLevel = "Info"}, 
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>());
            return services.BuildServiceProvider();
        }

        public static void RegisterFileBasedHlaMetadataDictionaryForTesting(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings)
        {
            services.RegisterHlaMetadataDictionary(
                _ => new HlaMetadataDictionarySettings(),
                fetchApplicationInsightsSettings,
                _ => new MacDictionarySettings()
            );
            
            services.SetUpMacDictionaryWithFileBackedRepository(fetchApplicationInsightsSettings, fetchMacDictionarySettings);

            services.RegisterConfiguration();
            // Replace Repositories with File-Backed equivalents.
            services.AddScoped<IHlaScoringMetadataRepository, FileBackedHlaScoringMetadataRepository>();
            services.AddScoped<IHlaMatchingMetadataRepository, FileBackedHlaMatchingMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, FileBackedAlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, FileBackedTceMetadataRepository>();
            services.AddScoped<IAlleleGroupsMetadataRepository, FileBackedAlleleGroupsMetadataRepository>();

            services.AddScoped(sp =>
            {
                var wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
                wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion);
                return wmdaHlaNomenclatureVersionAccessor;
            });
        }
        
        private static void RegisterConfiguration(this IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }
    }
}