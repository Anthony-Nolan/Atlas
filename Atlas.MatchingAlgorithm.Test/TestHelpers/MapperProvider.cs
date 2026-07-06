using Atlas.MatchingAlgorithm.Config;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers
{
    public static class MapperProvider
    {
        private static IMapper _mapper;
        
        public static IMapper Mapper => _mapper ??= AutoMapperConfig.CreateMapper("", NullLoggerFactory.Instance);
    }
}