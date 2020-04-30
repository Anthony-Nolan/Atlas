using FluentAssertions;
using Atlas.Utils.Core.Common;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Common
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("T", "t")]
        [TestCase("TitleCase", "titleCase")]
        [TestCase("camelCase", "camelCase")]
        public void GivenInput_ToCamelCase_ReturnsExpectedOutput(string input, string expected)
        {
            input.ToCamelCase().Should().Be(expected);
        }

        [TestCase(null, null)]
        [TestCase("", "1B2M2Y8AsgTpgAmY7PhCfg==")]
        [TestCase("aaa", "R7zlx09Yn0hn29V+nKn4CA==")]
        public void GivenInput_ToMD5Hash_ReturnsExpectedHash(string input, string expected)
        {
            input.ToMD5Hash().Should().Be(expected);
        }
    }
}
