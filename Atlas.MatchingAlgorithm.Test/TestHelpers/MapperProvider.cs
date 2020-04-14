using AutoMapper;
using Atlas.MatchingAlgorithm.Config;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers
{
    public static class MapperProvider
    {
        private static IMapper _mapper;
        
        public static IMapper Mapper => _mapper ?? (_mapper = AutomapperConfig.CreateMapper());
    }
}