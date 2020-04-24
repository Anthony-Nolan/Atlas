using System;
using FluentAssertions;
using Atlas.Utils.Core.Common;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Common
{
    [TestFixture]
    public class EnumExtensionsTests
    {
        private class EnumExtensionsTestAttribute : Attribute
        {
            public readonly string SomeData;
            public EnumExtensionsTestAttribute(string someData)
            {
                SomeData = someData;
            }
        }

        public enum TestEnum
        {
            [EnumExtensionsTest(OptionOneAttributeData)]
            OptionOne,
            [EnumExtensionsTest(OptionTwoAttributeData)]
            OptionTwo,
            OptionWithNoAttribute
        }

        private const string OptionOneAttributeData = "this is attribute data for first option in enum";
        private const string OptionTwoAttributeData = "this is attribute data for second option in enum";

        [TestCase(TestEnum.OptionOne, OptionOneAttributeData)]
        [TestCase(TestEnum.OptionTwo, OptionTwoAttributeData)]
        public void GetAttribute_GivenValidEnum_GetsAttribute(TestEnum enumValue, string attributeData)
        {
            var attribute = enumValue.GetAttribute<EnumExtensionsTestAttribute>();
            attribute.Should().NotBeNull();
            attribute.SomeData.Should().Be(attributeData);
        }

        [Test]
        public void GetAttribute_GivenValidEnumValueWithoutAttribute_ReturnsNull()
        {
            const TestEnum enumValue = TestEnum.OptionWithNoAttribute;
            var attribute = enumValue.GetAttribute<EnumExtensionsTestAttribute>();
            attribute.Should().BeNull();
        }

        [Test]
        public void GetAttribute_GivenInvalidEnumValue_ThrowsInvalidOperationException()
        {
            const TestEnum enumValue = (TestEnum) (-1);
            Assert.Throws<InvalidOperationException>(() => enumValue.GetAttribute<EnumExtensionsTestAttribute>());
        }

        [TestCase(TestEnum.OptionOne, OptionOneAttributeData)]
        [TestCase(TestEnum.OptionTwo, OptionTwoAttributeData)]
        public void GetAttributeOrDefault_GivenValidEnum_GetsAttribute(TestEnum enumValue, string attributeData)
        {
            var attribute = enumValue.GetAttributeOrDefault<EnumExtensionsTestAttribute>();
            attribute.Should().NotBeNull();
            attribute.SomeData.Should().Be(attributeData);
        }

        [Test]
        public void GetAttributeOrDefault_GivenValidEnumValueWithoutAttribute_ReturnsNull()
        {
            const TestEnum enumValue = TestEnum.OptionWithNoAttribute;
            var attribute = enumValue.GetAttributeOrDefault<EnumExtensionsTestAttribute>();
            attribute.Should().BeNull();
        }

        [Test]
        public void GetAttributeOrDefault_GivenInvalidEnumValue_ReturnsNull()
        {
            const TestEnum enumValue = (TestEnum) (-1);
            var attribute = enumValue.GetAttributeOrDefault<EnumExtensionsTestAttribute>();
            attribute.Should().BeNull();
        }
    }
}