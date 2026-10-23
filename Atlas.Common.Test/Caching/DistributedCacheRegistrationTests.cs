using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    [TestFixture]
    public class DistributedCacheRegistrationTests
    {
        private Fixture fixture;

        [SetUp]
        public void SetUp() => fixture = new Fixture();

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RegisterDistributedCacheTypes_WithNoConnectionString_RegistersACacheThatKeepsNothing(string connectionString)
        {
            using var serviceProvider = ServicesWith(new DistributedCacheSettings { ConnectionString = connectionString }).BuildServiceProvider();

            serviceProvider.GetRequiredService<IDistributedCacheProvider>().Should().BeOfType<DisabledDistributedCacheProvider>()
                .Which.IsEnabled.Should().BeFalse();
        }

        [Test]
        public async Task RegisterDistributedCacheTypes_WithAConnectionString_RegistersTheCacheInRedis()
        {
            await using var serviceProvider = ServicesWith(UnreachableRedis()).BuildServiceProvider();

            serviceProvider.GetRequiredService<IDistributedCacheProvider>().Should().BeOfType<DistributedCacheProvider>()
                .Which.IsEnabled.Should().BeTrue();
        }

        [Test]
        public void RegisterDistributedCacheTypes_WithAConnectionStringThatIsNotValid_RegistersACacheThatKeepsNothingAndLogsAnError()
        {
            var settings = new DistributedCacheSettings { ConnectionString = $"{UnreachableRedisEndpoint()},{fixture.Create<string>()}=1" };
            using var serviceProvider = ServicesWith(settings).BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IDistributedCacheProvider>();

            cache.Should().BeOfType<DisabledDistributedCacheProvider>();
            serviceProvider.GetFakeLogCollector().GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
        }

        [Test]
        public void RegisterDistributedCacheTypes_CalledTwice_RegistersOneCache()
        {
            var services = new ServiceCollection();

            services.RegisterDistributedCacheTypes(_ => new DistributedCacheSettings());
            services.RegisterDistributedCacheTypes(_ => new DistributedCacheSettings());

            services.Count(descriptor => descriptor.ServiceType == typeof(IDistributedCacheProvider)).Should().Be(1);
        }

        [Test]
        public void RegisterDistributedCacheTypes_WithLoggingAndApplicationInsights_AllowsServiceProviderValidationOnBuild()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));
            services.RegisterDistributedCacheTypes(_ => UnreachableRedis());

            var build = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            build.Should().NotThrow();
        }

        /// <summary>
        /// The real Redis client, against a port that nothing listens on: the cache is down, and a read is a fast miss, not
        /// an exception or a wait.
        /// </summary>
        [Test]
        public async Task GetAsync_WhenRedisCannotBeReached_ReturnsNullWithinSeconds()
        {
            await using var serviceProvider = ServicesWith(UnreachableRedis()).BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<IDistributedCacheProvider>();
            var stopwatch = Stopwatch.StartNew();

            var value = await cache.GetAsync(new DistributedCacheKey(fixture.Create<string>(), fixture.Create<string>()));

            value.Should().BeNull();
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
            serviceProvider.GetFakeLogCollector().GetSnapshot().Should().Contain(log => log.Level == LogLevel.Warning,
                "the read failed, so the null is a fault treated as a miss, not a miss from Redis");
        }

        /// <summary>
        /// The Redis client puts the key into the messages of its exceptions, unless it is told not to. The fault is logged
        /// with its exception, and the id of a key can hold HLA typing data.
        /// </summary>
        [Test]
        public async Task GetAsync_WhenRedisCannotBeReached_LogsTheFaultWithoutTheKeyId()
        {
            await using var serviceProvider = ServicesWith(UnreachableRedis()).BuildServiceProvider();
            var key = new DistributedCacheKey(fixture.Create<string>(), fixture.Create<string>());

            await serviceProvider.GetRequiredService<IDistributedCacheProvider>().GetAsync(key);

            var fault = serviceProvider.GetFakeLogCollector().GetSnapshot().Should().ContainSingle(log => log.Exception != null).Which;
            fault.Exception.ToString().Should().NotContain(key.Id);
        }

        private static ServiceCollection ServicesWith(DistributedCacheSettings settings)
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddFakeLogging());
            services.AddSingleton(Substitute.For<IDistributedCacheMetrics>());
            services.RegisterDistributedCacheTypes(_ => settings);
            return services;
        }

        private static DistributedCacheSettings UnreachableRedis() => new() { ConnectionString = $"{UnreachableRedisEndpoint()},connectTimeout=500" };

        /// <summary>A port on this machine that nothing listens on: it is bound, then released at once.</summary>
        private static string UnreachableRedisEndpoint()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return $"{IPAddress.Loopback}:{port}";
        }
    }
}
