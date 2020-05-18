using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Common.Utils.Extensions
{
    public static class DependencyInjectionUtils
    {
        public static void RegisterOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }
    }
}
