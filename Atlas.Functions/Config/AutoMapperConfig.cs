using System.Linq;
using System.Reflection;
using Atlas.Common.Utils.Extensions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atlas.Functions.Config;

internal class AutoMapperConfig
{
    public static IMapper CreateMapper(string licenseKey = null, ILoggerFactory loggerFactory = null)
    {
        var assemblyNames = Assembly.GetExecutingAssembly()
            .LoadAtlasAssemblies()
            .Select(a => a.GetName().Name)
            .ToArray();

        var config = new MapperConfiguration(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                cfg.LicenseKey = licenseKey;
            }

            cfg.AddMaps(assemblyNames);
        }, loggerFactory ?? NullLoggerFactory.Instance);

        return config.CreateMapper();
    }
}