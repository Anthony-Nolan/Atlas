using Atlas.MatchingAlgorithm.Test.TestHelpers;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Mapping
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