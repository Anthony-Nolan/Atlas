using Atlas.MatchingAlgorithm.Config;
using AutoMapper;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers
{
    public static class MapperProvider
    {
        private static IMapper _mapper;
        
        public static IMapper Mapper => _mapper ?? (_mapper = AutoMapperConfig.CreateMapper());
    }
}