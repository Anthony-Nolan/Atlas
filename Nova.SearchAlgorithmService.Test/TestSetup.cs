using AutoMapper;
using Nova.SearchAlgorithmService.Config;
using NUnit.Framework;

namespace Nova.SearchAlgorithmService.Test
{
    [SetUpFixture]
    public class TestSetup
    {
        public static IMapper Mapper { get; private set; }

        [OneTimeSetUp]
        public static void SetUp()
        {
            Mapper = AutomapperConfig.CreateMapper();
        }
    }
}
