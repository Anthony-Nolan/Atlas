using System;
using Atlas.Common.Caching;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;
using StackExchange.Redis;

namespace Atlas.Common.Test.Caching
{
    [TestFixture]
    public class RedisConnectionTests
    {
        /// <summary>The TLS port of Azure Managed Redis.</summary>
        private const int Port = 10000;

        private Fixture fixture;

        [SetUp]
        public void SetUp() => fixture = new Fixture();

        [Test]
        public void BuildOptions_KeepsTryingToConnectAfterAFailedAttempt()
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString()));

            options.AbortOnConnectFail.Should().BeFalse();
        }

        [Test]
        public void BuildOptions_FailsEachCommandAtOnceWhileThereIsNoConnection()
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString()));

            options.BacklogPolicy.Should().BeSameAs(BacklogPolicy.FailFast);
        }

        [Test]
        public void BuildOptions_UsesResp3()
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString()));

            options.Protocol.Should().Be(RedisProtocol.Resp3);
        }

        [Test]
        public void BuildOptions_LeavesKeyNamesOutOfExceptionMessages()
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString()));

            options.IncludeDetailInExceptions.Should().BeFalse();
        }

        [Test]
        public void BuildOptions_WithAManagedIdentity_UsesTls()
        {
            var settings = SettingsFor(NewConnectionString());
            settings.ManagedIdentityClientId = fixture.Create<string>();

            var options = RedisConnection.BuildOptions(settings);

            options.Ssl.Should().BeTrue();
        }

        [TestCase("", false)]
        [TestCase(",ssl=true", true)]
        public void BuildOptions_WithNoManagedIdentity_KeepsTheTlsSettingOfTheConnectionString(string tlsOption, bool expectedTls)
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString() + tlsOption));

            options.Ssl.Should().Be(expectedTls);
        }

        [Test]
        public void BuildOptions_WithAnUnknownOption_Throws()
        {
            var settings = SettingsFor($"{NewConnectionString()},{fixture.Create<string>()}=1");

            var build = () => RedisConnection.BuildOptions(settings);

            build.Should().Throw<ArgumentException>();
        }

        private string NewConnectionString() => $"{fixture.Create<string>()}:{Port}";

        private static DistributedCacheSettings SettingsFor(string connectionString) => new() { ConnectionString = connectionString };
    }
}
