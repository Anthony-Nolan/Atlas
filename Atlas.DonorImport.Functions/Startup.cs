using Atlas.DonorImport.ExternalInterface.DependencyInjection;
using Atlas.DonorImport.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.DonorImport.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // TODO: ATLAS-327: Inject settings
            builder.Services.RegisterDonorImport();
        }
    }
}