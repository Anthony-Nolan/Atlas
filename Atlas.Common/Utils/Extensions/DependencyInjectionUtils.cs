using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.Common.Utils.Extensions
{
    public static class DependencyInjectionUtils
    {
        public static void RegisterOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }

        public static Func<IServiceProvider, T> OptionsReaderFor<T>() where T : class, new()
        {
            return sp => sp.GetService<IOptions<T>>().Value;
        }
    }
}
