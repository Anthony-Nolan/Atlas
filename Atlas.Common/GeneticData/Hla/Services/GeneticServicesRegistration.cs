using Atlas.Common.GeneticData.Hla.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.Caching
{
    public static class GeneticServicesRegistration
    {
        public static void RegisterCommonGeneticServices(this IServiceCollection services)
        {
            services.AddScoped<IAlleleStringSplitterService, AlleleStringSplitterService>();
            services.AddScoped<IHlaCategorisationService, HlaCategorisationService>();
        }
    }
}
