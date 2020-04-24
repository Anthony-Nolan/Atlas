using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Atlas.Utils.Core.Config;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Config
{
    [TestFixture]
    public class AppSettingsConfigProviderTests
    {
        private AppSettingsConfigProvider<IConfigTest> underTest;

        public interface IConfigTest
        {
            string StringProperty { get; }
            [Optional]
            string OptionalReferenceProperty { get; }
            [Optional]
            int? OptionalNullableProperty { get; }
            int IntegerProperty { get; }
            int? NullableIntegerProperty { get; }
            DateTime DateProperty { get; }
            DateTime? NullableDateProperty { get; }
            IEnumerable<int> EnumerableIntProperty { get; }
            StringSplitOptions EnumStringSplitOptionsProperty { get; }
            StringSplitOptions? NullableEnumStringSplitOptionsProperty { get; }
            string InvalidProperty { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            underTest = new AppSettingsConfigProvider<IConfigTest>();
        }

        [Test]
        public void GivenInitialisedConfig_SetValuesDelimiter_ThrowsInvalidOperationException()
        {
            Action action = () =>
            {
                var x = underTest.Settings;
                underTest.ValuesDelimiter = '|';
            };
            action.ShouldThrow<InvalidOperationException>().Which.Message
                .Should().Be("The proxy class has already been created.");
        }

        [Test]
        public void GivenConfig_GetStringProperty_ReturnsExpectedValue()
        {
            underTest.Settings.StringProperty.Should().Be("abc123");
        }

        [Test]
        public void GivenConfig_GetIntProperty_ReturnsExpectedValue()
        {
            underTest.Settings.IntegerProperty.Should().Be(123);
        }

        [Test]
        public void GivenConfig_GetNullableIntProperty_ReturnsExpectedValue()
        {
            underTest.Settings.NullableIntegerProperty.Should().Be(123);
        }

        [Test]
        public void GivenConfig_GetOptionalReferenceProperty_ShouldBeNull()
        {
            underTest.Settings.OptionalReferenceProperty.Should().BeNull();
        }

        [Test]
        public void GivenConfig_GetOptionalNullableProperty_ShouldBeNull()
        {
            underTest.Settings.OptionalNullableProperty.Should().NotHaveValue();
        }

        [Test]
        public void GivenConfig_GetDateProperty_ReturnsExpectedValue()
        {
            underTest.Settings.DateProperty.Should().Be(DateTime.ParseExact("2009-06-15T13:45:30.0000000Z", "o", CultureInfo.InvariantCulture));
        }

        [Test]
        public void GivenConfig_GetNullableDateProperty_ReturnsExpectedValue()
        {
            underTest.Settings.NullableDateProperty.Should().Be(DateTime.ParseExact("2009-06-15T13:45:30.0000000Z", "o", CultureInfo.InvariantCulture));
        }

        [Test]
        public void GivenConfig_GetEnumerableIntProperty_ReturnsExpectedValue()
        {
            underTest.Settings.EnumerableIntProperty.Should().Equal(1, 2, 3);
        }

        [Test]
        public void GivenConfig_GetEnumStringSplitOptionsProperty_ReturnsExpectedValue()
        {
            underTest.Settings.EnumStringSplitOptionsProperty.Should().Be(StringSplitOptions.RemoveEmptyEntries);
        }

        [Test]
        public void GivenConfig_GetNullableEnumStringSplitOptionsProperty_ReturnsExpectedValue()
        {
            underTest.Settings.NullableEnumStringSplitOptionsProperty.Should().Be(StringSplitOptions.RemoveEmptyEntries);
        }

        [Test]
        public void GivenConfig_GetInvalidProperty_ThrowsInvalidOperationException()
        {
            Action action = () => underTest.Settings.InvalidProperty?.Trim();

            action.ShouldThrow<InvalidOperationException>().Which.Message
                .Should().Be("App setting 'InvalidProperty' not found.");
        }
    }
}
