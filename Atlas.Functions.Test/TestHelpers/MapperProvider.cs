using Atlas.Functions.Config;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atlas.Functions.Test.TestHelpers
{
    public static class MapperProvider
    {
        private static IMapper? _mapper;

        public static IMapper Mapper => _mapper ?? (_mapper = AutoMapperConfig.CreateMapper(null, NullLoggerFactory.Instance));
    }
}
