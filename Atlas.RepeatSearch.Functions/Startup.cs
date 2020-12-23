using Atlas.Common.ApplicationInsights;
using Atlas.RepeatSearch.ExternalInterface.DependencyInjection;
using Atlas.RepeatSearch.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.RepeatSearch.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterRepeatSearch(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                ConnectionStringReader("RepeatSearchSql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
        }
    }
}