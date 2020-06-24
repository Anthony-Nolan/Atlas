using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atlas.Common.Utils.Extensions
{
    /// <summary>
    /// The overall plan for using these methods looks like this:
    ///
    /// Some logic Module, <c cref="MyModule"/>
    ///  * Wants to use some settings <c cref="MyModuleSettings"/>.
    ///  * Has a DI registration point, which requires a <c cref="Func{IServiceProvider, MyModuleSettings}"/> parameter, called <c cref="fetcher"/>, say.
    ///  * Receives that <c cref="fetcher"/>, and passes it to <c cref="MakeOptionsAvailableForUse{MyModuleSettings}(fetcher)"/>
    ///  * <c cref="MyModule"/> can then have its classes declare a direct dependency on <c cref="MyModuleSettings"/>.
    /// 
    /// Raw-entry point object (e.g. Function App)
    ///  * Wants to use MyModule
    ///  * Has a Settings file, containing the relevant settings under some key, say "theModuleSettings".
    ///  * During startup calls <c cref="RegisterOptions{MyModuleSettings}("depdendentSettingsKey)"/>
    ///  * When calling the DI registration point of <c cref="MyModule"/>, calls <c cref="OptionsReaderFor{MyModuleSettings}()"/>, in order to create the fetcher Func referenced above.
    ///
    /// The raw entry points don't HAVE to use <see cref="RegisterOptions{T}"/> and <see cref="OptionsReaderFor{T}"/>!
    /// If they want to construct the Settings object in some other way, that's completely fine.
    /// And in Unit/Integration Tests that's very likely (where you may just pass in a Func returning a null, or a blank object. 
    /// </summary>
    public static class DependencyInjectionUtils
    {
        /// <summary>
        /// Uses Microsoft DI & IConfiguration frameworks to register that a settings object can be found in the App Settings, under a particular key.
        /// Note that this is expected to be used primarily in a set with <see cref="OptionsReaderFor{T}"/> and <see cref="MakeOptionsAvailableForUse{T}"/>
        /// <br/>
        /// This method should ONLY be used in a raw entry-point project.
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
        /// Uses Microsoft DI & IConfiguration frameworks to register a settings object as available to be dependended upon by other clases in the same project..
        /// Note that this is expected to be used primarily in a set with <see cref="RegisterOptions{T}"/> and <see cref="MakeOptionsAvailableForUse{T}"/>
        /// <br/>
        /// This is the ONLY method in the set that should be used in the logic Projects. The others should ONLY be used in raw entry-point projects.
        /// </summary>
        /// <remarks>
        /// This extension is primarily defined so that all of the management of Settings can be tied back to this file, so that Devs read all the docs here.
        /// </remarks>
        /// <typeparam name="T">Settings Object to be made available</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="fetcher">Func responsible for providing an instance of the Settings object. Likely the output of <see cref="OptionsReaderFor{T}"/></param>
        public static void MakeOptionsAvailableForUse<T>(this IServiceCollection services, Func<IServiceProvider, T> fetcher) where T : class
        {
            services.AddSingleton<T>(fetcher);
        }

        /// <summary>
        /// Builds an accessor which pulls the defined Settings object out of the Microsoft DI & Configuration framework
        /// Note that this is expected to be used primarily in a pair with <see cref="RegisterOptions{T}"/>, to pass a
        /// previously registered option on to the project dependency that needs it.
        /// The result should then be passed into <see cref="MakeOptionsAvailableForUse{T}"/> in the invoked logic project.
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
