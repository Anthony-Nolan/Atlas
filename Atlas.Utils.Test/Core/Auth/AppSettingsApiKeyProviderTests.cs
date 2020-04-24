using System.Threading.Tasks;
using FluentAssertions;
using Atlas.Utils.Core.Auth;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.Auth
{
    [TestFixture]
    public class AppSettingsApiKeyProviderTests
    {
        // These match the values in app.config
        private const string ValidKey = "abc123";
        private const string DisabledKey = "xyz789";

        private readonly AppSettingsApiKeyProvider underTest = new AppSettingsApiKeyProvider();

        [Test]
        public async Task GivenDisabledApiKey_IsValid_ReturnsFalse()
        {
            (await underTest.IsValid(DisabledKey)).Should().BeFalse();
        }

        [Test]
        public async Task GivenInvalidApiKey_IsValid_ReturnsFalse()
        {
            (await underTest.IsValid("Other")).Should().BeFalse();
        }

        [Test]
        public async Task GivenValidApiKey_IsValid_ReturnsTrue()
        {
            (await underTest.IsValid(ValidKey)).Should().BeTrue();
        }
    }
}
