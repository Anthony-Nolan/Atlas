using Atlas.Functions.Test.TestHelpers;
using NUnit.Framework;

namespace Atlas.Functions.Test.Mapping;

[TestFixture]
public class MappingTests
{
    [Test]
    public void AssertMappingConfigurationValid()
    {
        MapperProvider.Mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}