using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public static class GeneticServicesRegistration
    {
        public static void RegisterCommonGeneticServices(this IServiceCollection services)
        {
            services.AddScoped<IAlleleNamesExtractor, AlleleNamesExtractor>();
            services.AddScoped<IHlaCategorisationService, HlaCategorisationService>();
        }
    }
}
