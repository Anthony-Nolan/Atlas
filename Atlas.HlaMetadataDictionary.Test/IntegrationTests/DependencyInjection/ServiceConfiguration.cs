using System;
using Atlas.Common.ApplicationInsights;
using System.Collections.Generic;
using System.IO;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.Settings;
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
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(sp => new ApplicationInsightsSettings { LogLevel = "Info" });
            return services.BuildServiceProvider();
        }

        public static void RegisterFileBasedHlaMetadataDictionaryForTesting(this IServiceCollection services, Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            Func<IServiceProvider, string> blank = _ => ""; 
            services.RegisterHlaMetadataDictionary(blank, blank, fetchApplicationInsightsSettings, _ => null); //This is actually used.
            
            
            services.RegisterOptions<MacImportSettings>("MacImport");
            SetUpConfiguration(services);
            services.RegisterMacDictionary(
                fetchApplicationInsightsSettings,
                DependencyInjectionUtils.OptionsReaderFor<MacImportSettings>()
            );

            services.AddScoped<IMacRepository, FileBackedMacDictionaryRepository>();
            
            // Replace Repositories with File-Backed equivalents.
            SetUpConfiguration(services);
            services.AddScoped<IHlaScoringMetadataRepository, FileBackedHlaScoringMetadataRepository>();
            services.AddScoped<IHlaMatchingMetadataRepository, FileBackedHlaMatchingMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, FileBackedAlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, FileBackedTceMetadataRepository>();
            
            // Mac Dictionary Stubs
            // TODO: ATLAS-320 Move this to MacDictionary Tests, along with any tests that actually belong over there.
            // After that migration, this may or may not still be needed in here, and/or in MatchingAlgorithm.Tests
            // If it is, expose this as a Test Registration in the MacDictionary project.

            services.AddScoped(sp =>
            {
                var wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
                wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(Constants.SnapshotHlaNomenclatureVersion);
                return wmdaHlaNomenclatureVersionAccessor;
            });
        }
        
        private static void SetUpConfiguration(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
        }
    }
}