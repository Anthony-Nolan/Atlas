using Atlas.Common.Utils.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.Common
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [TestCase(null, null)]
        [TestCase("", "1B2M2Y8AsgTpgAmY7PhCfg==")]
        [TestCase("aaa", "R7zlx09Yn0hn29V+nKn4CA==")]
        public void GivenInput_ToMD5Hash_ReturnsExpectedHash(string input, string expected)
        {
            input.ToMd5Hash().Should().Be(expected);
        }
    }
}
