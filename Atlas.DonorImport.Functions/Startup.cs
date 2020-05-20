using Atlas.DonorImport.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Atlas.DonorImport.Functions.Startup))]

namespace Atlas.DonorImport.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterDonorImportTypes();
        }
    }
}