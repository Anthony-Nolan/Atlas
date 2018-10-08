using Autofac;
using Hangfire;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Services.DonorImport;
using Owin;
using System.Configuration;

namespace Nova.SearchAlgorithm.Config
{
    public static class HangfireConfig
    {
        public static void ConfigureHangfire(this IAppBuilder app, IContainer container)
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage(ConfigurationManager.ConnectionStrings["HangfireSqlConnectionString"].ConnectionString);
            GlobalConfiguration.Configuration.UseAutofacActivator(container);

            app.UseHangfireDashboard();
            app.UseHangfireServer();
            
            BackgroundJob.Enqueue<IAntigenCachingService>(antigenCachingService => antigenCachingService.GenerateAntigenCache());
            BackgroundJob.Enqueue<IHlaMatchingLookupRepository>(hlaMatchingLookupRepository => hlaMatchingLookupRepository.LoadDataIntoMemory());
            BackgroundJob.Enqueue<IAlleleNamesLookupRepository>(alleleNamesLookupRepository => alleleNamesLookupRepository.LoadDataIntoMemory());
            BackgroundJob.Enqueue<IHlaScoringLookupRepository>(hlaScoringLookupRepository => hlaScoringLookupRepository.LoadDataIntoMemory());
            BackgroundJob.Enqueue<IDpb1TceGroupsLookupRepository>(dpb1TceGroupsLookupRepository => dpb1TceGroupsLookupRepository.LoadDataIntoMemory());
        }
    }
}