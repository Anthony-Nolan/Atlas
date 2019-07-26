using Nova.SearchAlgorithm.Test.TestHelpers;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Mapping
{
    [TestFixture]
    public class MappingTests
    {
        [Test]
        public void AssertMappingConfigurationValid()
        {
            MapperProvider.Mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}