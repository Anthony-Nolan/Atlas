using System.Collections.Generic;
using FluentAssertions;
using Atlas.Utils.Core.Common;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Common
{
    [TestFixture]
    public class DictionaryExtensionsTests
    {
        [Test]
        public void GivenDictionaryWithoutGivenKey_GetOrDefaultWithDefault_ReturnsDefaultValue()
        {
            var dict = new Dictionary<string, string>();

            dict.GetOrDefault("key", "value").Should().Be("value");
        }

        [Test]
        public void GivenDictionaryWithKey_GetOrDefaultWithDefault_ReturnsDictValue()
        {
            var dict = new Dictionary<string, string> { { "key", "value" } };

            dict.GetOrDefault("key", "some default").Should().Be("value");
        }

        [Test]
        public void GivenDictionaryWithoutGivenKey_GetOrDefaultWithoutDefault_ReturnsDefaultTypeValue()
        {
            var dict = new Dictionary<string, int>();

            dict.GetOrDefault("key").Should().Be(0);
        }

        [Test]
        public void GivenDictionaryWithKey_GetOrDefaultWithoutDefault_ReturnsDictValue()
        {
            var dict = new Dictionary<string, string> { { "key", "value" } };

            dict.GetOrDefault("key").Should().Be("value");
        }
    }
}
