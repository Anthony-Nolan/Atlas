using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Data.Context;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;

namespace Nova.SearchAlgorithm.Test.Validation.DependencyInjection
{
    /// <summary>
    /// Used to set up dependency injection for the validation test framework itself.
    /// Note that the injected configuration is for the validation project - for the in memory api the configuration is set up elsewhere
    /// </summary>
    public static class DependencyInjection
    {
        public static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets("841c4767-6a3e-4edc-bae0-f657a980f940")
                .Build();

            services.AddSingleton<IConfiguration>(sp => configuration);
            services.Configure<ValidationTestSettings>(configuration.GetSection("Testing"));
            
            // As some of the meta donors are generated dynamically at runtime, the repository must be a singleton
            // Otherwise, the meta-donors will be regenerated on lookup, and no longer match the ones in the database
            services.AddSingleton<IMetaDonorsData, MetaDonorsData>();
            services.AddSingleton<IMetaDonorRepository, MetaDonorRepository>();

            services.AddScoped<IAlleleRepository, AlleleRepository>();
            services.AddScoped<ITestDataService, TestDataService>();
            
            services.AddScoped<IPatientDataFactory, PatientDataFactory>();
            services.AddScoped<IMultiplePatientDataFactory, MultiplePatientDataFactory>();
            services.AddScoped<IStaticDataProvider, StaticDataProvider>();
            
            services.AddScoped<IMetaDonorSelector, MetaDonorSelector>();
            services.AddScoped<IDatabaseDonorSelector, DatabaseDonorSelector>();
            services.AddScoped<IPatientHlaSelector, PatientHlaSelector>();

            RegisterDataServices(services);

            return services.BuildServiceProvider();
        }

        private static void RegisterDataServices(IServiceCollection services)
        {
            services.AddScoped(sp =>
                new ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            services.AddScoped(sp =>
                new Data.Persistent.ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"])
            );

            services.AddScoped<ITestDataRepository, TestDataRepository>();
        }
    }
}