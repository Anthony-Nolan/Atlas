using System.Linq;
using System.Reflection;
using AutoMapper;
using Nova.Utils.Reflection;

namespace Nova.SearchAlgorithmService.Config
{
    public class AutomapperConfig
    {
        public static IMapper CreateMapper()
        {
            var assemblyNames = Assembly.GetExecutingAssembly()
                .LoadNovaAssemblies()
                .Select(a => a.GetName().Name)
                .ToArray();

            var config = new MapperConfiguration(cfg => { cfg.AddProfiles(assemblyNames); });
            return config.CreateMapper();
        }
    }
}