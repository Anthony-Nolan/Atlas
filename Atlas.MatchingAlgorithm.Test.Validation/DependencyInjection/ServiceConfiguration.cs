using System.Reflection;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.StaticDataSelection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchingAlgorithm.Test.Validation.DependencyInjection
{
    /// <summary>
    /// Used to set up dependency injection for the validation test framework itself.
    /// Note that the injected configuration is for the validation project - for the in memory api the configuration is set up elsewhere
    /// </summary>
    internal static class ServiceConfiguration
    {
        public static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            /*
             * Note that because the MatchAlg Validation tests run a virtual Server running the API project,
             * the appSettings file referenced here (which is the one in the Validation folder) needs to define
             * all of the configuration that the API project looks for, as well as any settings that the
             * Validation code wants to use directly.
             * (Comment duplicated in Validation.ServiceConfiguration, Validation.appSettings, Api.Startup(twice), Api.appSettings)",
             */
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
            services.RegisterAsOptions<ValidationTestSettings>("Testing");
            // As some of the meta donors are generated dynamically at runtime, the repository must be a singleton
            // Otherwise, the meta-donors will be regenerated on lookup, and no longer match the ones in the database
            services.AddSingleton<IMetaDonorsData, MetaDonorsData>();
            services.AddSingleton<IMetaDonorRepository, MetaDonorRepository>();

            // Services are fetched once per scenario, from the scope that the "BeforeScenario" hook creates for it.
            services.AddTransient<IAlleleRepository, AlleleRepository>();
            services.AddTransient<ITestDataService, TestDataService>();

            services.AddTransient<IPatientDataFactory, PatientDataFactory>();
            services.AddTransient<IMultiplePatientDataFactory, MultiplePatientDataFactory>();
            services.AddTransient<IStaticDataProvider, StaticDataProvider>();

            services.AddTransient<IMetaDonorSelector, MetaDonorSelector>();
            services.AddTransient<IDatabaseDonorSelector, DatabaseDonorSelector>();
            services.AddTransient<IPatientHlaSelector, PatientHlaSelector>();

            services.RegisterDataServices();

            return services.BuildServiceProvider();
        }

        private static void RegisterDataServices(this IServiceCollection services)
        {
            services.AddScoped(sp =>
                new ContextFactory().Create(ConnectionStringReader("SqlA")(sp))
            );

            services.AddScoped(sp =>
                new Data.Persistent.Context.ContextFactory().Create(ConnectionStringReader("PersistentSql")(sp))
            );

            services.AddScoped(sp =>
                new DonorImport.Data.Context.ContextFactory().Create(ConnectionStringReader("DonorSql")(sp))
            );

            services.AddScoped<ITestDataRepository, TestDataRepository>();
        }
    }
}