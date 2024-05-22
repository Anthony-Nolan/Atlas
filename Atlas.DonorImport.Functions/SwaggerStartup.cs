using System.IO;
using System.Reflection;
using Atlas.DonorImport.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.Functions
{
    internal class SwashBuckleStartup 
    {
        public static void Configure(IServiceCollection services)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            services.AddSwashBuckle(executingAssembly, opts =>
            {
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{executingAssembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(GetTopBinPath(executingAssembly), xmlFile);
                opts.XmlPath = xmlPath;
            });
        }

        private static string GetTopBinPath(Assembly executingAssembly)
        {
            var assemblyLocation = executingAssembly.Location;
            return Directory.GetParent(Directory.GetParent(assemblyLocation)?.FullName ?? string.Empty)?.FullName ?? string.Empty;
        }
    }
}