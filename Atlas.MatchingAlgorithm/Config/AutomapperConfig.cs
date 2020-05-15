using System.Linq;
using System.Reflection;
using Atlas.Common.Utils.Extensions;
using AutoMapper;

namespace Atlas.MatchingAlgorithm.Config
{
    public class AutomapperConfig
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