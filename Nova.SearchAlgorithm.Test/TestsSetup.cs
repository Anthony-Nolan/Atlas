using AutoMapper;
using Nova.SearchAlgorithm.Config;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test
{
    [SetUpFixture]
    public class TestsSetup
    {
        public static IMapper Mapper { get; private set; }

        [OneTimeSetUp]
        public static void SetUp()
        {
            Mapper = AutomapperConfig.CreateMapper();
        }
    }
}
