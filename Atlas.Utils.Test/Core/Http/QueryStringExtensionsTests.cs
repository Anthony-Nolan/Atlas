using System;
using System.Collections.Generic;
using FluentAssertions;
using Atlas.Utils.Core.Http;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Http
{
    [TestFixture]
    public class QueryStringExtensionsTests
    {
        private enum TestEnum
        {
            One = 1,
        }

        [TestCase(123)]
        [TestCase(TestEnum.One)]
        [TestCase("123")]
        public void GivenInvalidInput_ToQueryString_ShouldThrowArgumentException(object input)
        {
            Action action = () => 123.ToQueryStringParams();
            action.ShouldThrow<ArgumentException>()
                .WithMessage("Can only convert a non-enumerable type with properties.");
        }

        [TestCaseSource(nameof(ToQueryStringData))]
        public void GivenObject_ToQueryString_ReturnsExpectedResult(object input, string expected)
        {
            input.ToQueryStringParams().ToQueryString().Should().Be(expected);
        }

        private static IEnumerable<TestCaseData> ToQueryStringData()
        {
            yield return new TestCaseData(new { Thing1 = "Value" }, "?thing1=Value");
            yield return new TestCaseData(new { Thing2 = 123 }, "?thing2=123");
            yield return new TestCaseData(new { Thing3 = (int?)123 }, "?thing3=123");
            yield return new TestCaseData(new { Thing4 = (string)null }, string.Empty);
            yield return new TestCaseData(new { Thing5 = TestEnum.One }, "?thing5=One");
            yield return new TestCaseData(new { Thing6 = new { SubThing = 123 } }, "?thing6.subThing=123");
            yield return new TestCaseData(new { Thing7 = new[] { 1, 2 } }, "?thing7[0]=1&thing7[1]=2");
            yield return new TestCaseData(
                new { Thing8 = new[] { new { SubThing = 123 }, new { SubThing = 456 } } },
                "?thing8[0].subThing=123&thing8[1].subThing=456");
            yield return new TestCaseData(
                new { Thing9 = new DateTime(2017, 1, 1, 12, 0, 0, DateTimeKind.Utc) },
                "?thing9=2017-01-01T12%3A00%3A00.0000000Z");
        }
    }
}
