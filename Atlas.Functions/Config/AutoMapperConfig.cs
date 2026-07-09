using System.Linq;
using System.Reflection;
using Atlas.Common.Utils.Extensions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atlas.Functions.Config;

internal class AutoMapperConfig
{
    public static IMapper CreateMapper(string licenseKey, ILoggerFactory loggerFactory)
    {
        var assemblyNames = Assembly.GetExecutingAssembly()
            .LoadAtlasAssemblies()
            .Select(a => a.GetName().Name)
            .ToArray();

        var config = new MapperConfiguration(cfg =>
            {
                cfg.LicenseKey = licenseKey;
                cfg.AddMaps(assemblyNames);
            }, loggerFactory
        );

        return config.CreateMapper();
    }
}