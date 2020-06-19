using System;
using System.Collections.Generic;
using System.IO;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
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
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<MacImportSettings>("MacImport");
            SetUpConfiguration(services);
            services.RegisterMacDictionary(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacImportSettings>()
            );
            return services.BuildServiceProvider();
        }

        public static void RegisterFileBasedHlaMetadataDictionaryForTesting(this IServiceCollection services, Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings)
        {
            // Replace Repositories with File-Backed equivalents.
            services.AddScoped<IHlaScoringMetadataRepository, FileBackedHlaScoringMetadataRepository>();
            services.AddScoped<IHlaMatchingMetadataRepository, FileBackedHlaMatchingMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, FileBackedAlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, FileBackedTceMetadataRepository>();

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