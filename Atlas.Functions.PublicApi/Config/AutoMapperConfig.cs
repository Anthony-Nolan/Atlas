using System.Linq;
using AutoMapper;
using System.Reflection;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Functions.PublicApi.Config
{
    internal class AutoMapperConfig
    {
        public static IMapper CreateMapper()
        {
            var assemblyNames = Assembly.GetExecutingAssembly()
                .LoadAtlasAssemblies()
                .Select(a => a.GetName().Name)
                .ToArray();

            var config = new MapperConfiguration(cfg => { cfg.AddMaps(assemblyNames); });
            return config.CreateMapper();
        }
    }
}
