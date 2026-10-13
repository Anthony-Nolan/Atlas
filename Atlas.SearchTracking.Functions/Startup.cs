using Atlas.Common.Utils.Extensions;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Functions.Config;
using Atlas.SearchTracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.SearchTracking.Functions
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddDbContext<ISearchTrackingContext, SearchTrackingContext>(options =>
            {
                options.UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings:PersistentSql"));
            });

            services.AddScoped<ISearchTrackingEventProcessor, SearchTrackingEventProcessor>();
            services.AddScoped<ISearchRequestRepository, SearchRequestRepository>();
            services.AddScoped<IMatchPredictionRepository, MatchPredictionRepository>();
            services.AddScoped<ISearchRequestMatchingAlgorithmAttemptsRepository, SearchRequestMatchingAlgorithmAttemptsRepository>();
            services.RegisterAsOptions<SearchTrackingServiceBusSettings>("MessagingServiceBus");
            services.AddSingleton(sp => AutoMapperConfig.CreateMapper(
                sp.GetRequiredService<IConfiguration>()["AutoMapper:LicenseKey"] ?? throw new InvalidOperationException("AutoMapper license key is required"),
                sp.GetRequiredService<ILoggerFactory>()));
        }
    }
}
