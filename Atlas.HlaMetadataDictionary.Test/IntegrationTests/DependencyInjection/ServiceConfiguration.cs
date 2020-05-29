using System;
using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MultipleAlleleCodeDictionary.HlaService;
using Atlas.MultipleAlleleCodeDictionary.HlaService.Models;
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
            services.RegisterHlaMetadataDictionary(
                blank, //These blank config values won't be used, because all they are all (indirectly) overridden, below.
                blank, 
                blank, 
                blank,
                fetchApplicationInsightsSettings); //This is actually used.

            // Replace Repositories with File-Backed equivalents.
            services.AddScoped<IHlaScoringLookupRepository, FileBackedHlaScoringLookupRepository>();
            services.AddScoped<IHlaMatchingLookupRepository, FileBackedHlaMatchingLookupRepository>();
            services.AddScoped<IAlleleNamesLookupRepository, FileBackedAlleleNamesLookupRepository>();
            services.AddScoped<IDpb1TceGroupsLookupRepository, FileBackedTceLookupRepository>();

            // Mac Dictionary Stubs
            // TODO: ATLAS-320 Move this to MacDictionary Tests, along with any tests that actually belong over there.
            // After that migration, this may or may not still be needed in here, and/or in MatchingAlgorithm.Tests
            // If it is, expose this as a Test Registration in the MacDictionary project.
            var mockHlaServiceClient = Substitute.For<IHlaServiceClient>();
            mockHlaServiceClient.GetAntigens(Arg.Any<Locus>(), Arg.Any<bool>()).Returns(new List<Antigen>());
            services.AddScoped(sp => mockHlaServiceClient);
        }
    }
}