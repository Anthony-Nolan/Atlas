using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    public static class DependencyInjection
    {
        public static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();

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
                new Data.Context.ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlA"])
            );

            services.AddScoped(sp =>
                new Data.Persistent.ContextFactory().Create(sp.GetService<IConfiguration>().GetSection("ConnectionStrings")["PersistentSql"])
            );

            services.AddScoped<ITestDataRepository, TestDataRepository>();
        }
    }
}