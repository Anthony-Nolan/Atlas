using Atlas.MultipleAlleleCodeDictionary.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MultipleAlleleCodeDictionary.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterMultipleAlleleCodeDictionaryTypes();
        }
    }
}