using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.Matching.Services
{
    public static class MatchingServicesRegistration
    {
        public static void RegisterCommonMatchingServices(this IServiceCollection services)
        {
            services.AddScoped<ILocusMatchCalculator, LocusMatchCalculator>();
            services.AddScoped<IStringBasedLocusMatchCalculator, StringBasedLocusMatchCalculator>();
        }
    }
}
