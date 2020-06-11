using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.Common.Utils.Extensions
{
    public static class DependencyInjectionUtils
    {
        /// <summary>
        /// Uses Microsoft DI & IConfiguration frameworks to register that a settings object can be found the the App Settings.
        /// Note that this is expected to be used primarily in a pair with <see cref="OptionsReaderFor{T}"/>.
        /// <br/>
        /// Both methods should ONLY be used in a raw entry-point project.
        /// All other projects should be receiving their configuration from the invoking projects.
        /// </summary>
        /// <typeparam name="T">Settings Object to be populated</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="sectionName">Settings key from which to read the Settings</param>
        public static void RegisterOptions<T>(this IServiceCollection services, string sectionName) where T : class
        {
            services.AddOptions<T>().Configure<IConfiguration>((settings, config) => { config.GetSection(sectionName).Bind(settings); });
        }

        /// <summary>
        /// Builds an accessor which pulls the defined Settings object out of the Microsoft DI & Configuration framework
        /// Note that this is expected to be used primarily in a pair with <see cref="RegisterOptions{T}"/>, to pass a
        /// previously registered option on to the project dependency that needs it.
        /// <br/>
        /// Both methods should ONLY be used in a raw entry-point project.
        /// All other projects should be receiving their configuration from the invoking projects.
        /// </summary>
        /// <typeparam name="T">Settings Object to be accessed</typeparam>
        public static Func<IServiceProvider, T> OptionsReaderFor<T>() where T : class, new()
        {
            return sp => sp.GetService<IOptions<T>>().Value;
        }
    }
}
