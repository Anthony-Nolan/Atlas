using AutoMapper;
using Nova.SearchAlgorithm.Config;

namespace Nova.SearchAlgorithm.Test.TestHelpers
{
    public static class MapperProvider
    {
        private static IMapper _mapper;
        
        public static IMapper Mapper => _mapper ?? (_mapper = AutomapperConfig.CreateMapper());
    }
}