using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.DonorImport.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // TODO: ATLAS-327: Inject settings
            builder.Services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            builder.Services.RegisterDonorImport(sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value);
        }
    }
}