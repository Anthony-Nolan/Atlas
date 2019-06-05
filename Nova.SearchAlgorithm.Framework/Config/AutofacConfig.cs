using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Autofac;
using Nova.Utils.Reflection;
using Owin;

namespace Nova.SearchAlgorithm.Config
{
    public static class AutofacConfig
    {
        [ExcludeFromCodeCoverage]
        public static IContainer ConfigureAutofac(this IAppBuilder app, IContainer container = null)
        {
            if (container == null)
            {
                var builder = new ContainerBuilder();

                builder.RegisterType<MemoryCache>().As<IMemoryCache>().WithParameter("optionsAccessor", new MemoryCacheOptions()).SingleInstance();

                var assemblies = Assembly.GetExecutingAssembly().LoadNovaAssemblies().ToArray();
                builder.RegisterAssemblyModules(assemblies);

                container = builder.Build();
            }
            app.UseAutofacLifetimeScopeInjector(container);
            return container;
        }
    }
}
