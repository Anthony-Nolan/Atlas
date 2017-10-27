using AutoMapper;
using Nova.TemplateService.Config;
using NUnit.Framework;

namespace Nova.TemplateService.Tests
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
