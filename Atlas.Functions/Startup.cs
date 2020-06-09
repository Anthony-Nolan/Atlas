using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterMatchingAlgorithmOrchestration();
            builder.Services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            builder.Services.RegisterOptions<MacImportSettings>("MacImport");
            builder.Services.RegisterMacDictionary(RegisterApplicationInsightsSettings, RegisterMacImportSettings);
        }
        
        private ApplicationInsightsSettings RegisterApplicationInsightsSettings(IServiceProvider sp)
        {
            return sp.GetService<IOptions<ApplicationInsightsSettings>>().Value;
        }
        
        private MacImportSettings RegisterMacImportSettings(IServiceProvider sp)
        {
            return sp.GetService<IOptions<MacImportSettings>>().Value;
        }
    }
}