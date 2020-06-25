using System;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
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
            Func<IServiceProvider, string> blank = _ => ""; // These configs are not needed for their file-backed replacements, so we can use blank settings.
            services.RegisterHlaMetadataDictionary(blank, blank, fetchApplicationInsightsSettings, _ => null);
            
            // Replace Repositories with File-Backed equivalents.
            services.AddScoped<IHlaScoringMetadataRepository, FileBackedHlaScoringMetadataRepository>();
            services.AddScoped<IHlaMatchingMetadataRepository, FileBackedHlaMatchingMetadataRepository>();
            services.AddScoped<IAlleleNamesMetadataRepository, FileBackedAlleleNamesMetadataRepository>();
            services.AddScoped<IDpb1TceGroupsMetadataRepository, FileBackedTceMetadataRepository>();
            
            services.AddScoped(sp => Substitute.For<IMacDictionary>());

            services.AddScoped(sp =>
            {
                var wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
                wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().Returns(Constants.SnapshotHlaNomenclatureVersion);
                return wmdaHlaNomenclatureVersionAccessor;
            });
        }
    }
}