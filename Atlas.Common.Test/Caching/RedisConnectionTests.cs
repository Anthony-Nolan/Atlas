using System;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
        private FakeTimeProvider timeProvider;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            timeProvider = new FakeTimeProvider();
        }

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
        public void BuildOptions_FailsACommandThatRedisDoesNotAnswerWithinTheCommandTimeout()
        {
            var options = RedisConnection.BuildOptions(SettingsFor(NewConnectionString()));

            options.AsyncTimeout.Should().Be((int)RedisConnection.CommandTimeout.TotalMilliseconds);
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

        [Test]
        public async Task WithBackOffAfterFailure_WhenTheAttemptSucceeds_ReturnsTheConnection()
        {
            var connection = Substitute.For<IConnectionMultiplexer>();
            var connect = Substitute.For<Func<Task<IConnectionMultiplexer>>>();
            connect().Returns(connection);

            var factory = RedisConnection.WithBackOffAfterFailure(connect, timeProvider);

            (await factory()).Should().BeSameAs(connection);
        }

        [Test]
        public async Task WithBackOffAfterFailure_WhenTheAttemptFails_ThrowsItsFault()
        {
            var fault = fixture.Create<Exception>();
            var factory = RedisConnection.WithBackOffAfterFailure(FailingConnect(fault), timeProvider);

            (await factory.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(fault);
        }

        /// <summary>
        /// RedisCache calls the factory again on each read or write after a failed attempt, and an attempt can take many
        /// seconds to fail: e.g. one that cannot get a Microsoft Entra ID token.
        /// </summary>
        [Test]
        public async Task WithBackOffAfterFailure_WithinTheBackOffAfterAFailedAttempt_FailsWithoutAnAttempt()
        {
            var connect = FailingConnect(fixture.Create<Exception>());
            var factory = RedisConnection.WithBackOffAfterFailure(connect, timeProvider);
            await factory.Should().ThrowAsync<Exception>();
            timeProvider.Advance(RedisConnection.FailedConnectBackOff - TimeSpan.FromTicks(1));
            connect.ClearReceivedCalls();

            await factory.Should().ThrowAsync<RedisConnectionException>();

            await connect.DidNotReceive().Invoke();
        }

        [Test]
        public async Task WithBackOffAfterFailure_AfterTheBackOff_AttemptsAgain()
        {
            var connect = FailingConnect(fixture.Create<Exception>());
            var factory = RedisConnection.WithBackOffAfterFailure(connect, timeProvider);
            await factory.Should().ThrowAsync<Exception>();
            timeProvider.Advance(RedisConnection.FailedConnectBackOff);
            connect.ClearReceivedCalls();

            await factory.Should().ThrowAsync<Exception>();

            await connect.Received(1).Invoke();
        }

        private string NewConnectionString() => $"{fixture.Create<string>()}:{Port}";

        private static DistributedCacheSettings SettingsFor(string connectionString) => new() { ConnectionString = connectionString };

        private static Func<Task<IConnectionMultiplexer>> FailingConnect(Exception fault)
        {
            var connect = Substitute.For<Func<Task<IConnectionMultiplexer>>>();
            connect().ThrowsAsync(fault);
            return connect;
        }
    }
}
