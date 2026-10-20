using System;
using Atlas.Common.Caching;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    [TestFixture]
    public class DistributedCacheKeyTests
    {
        private Fixture fixture;

        [SetUp]
        public void SetUp() => fixture = new Fixture();

        [Test]
        public void ToString_JoinsTheCacheNameAndTheIdWithAColon()
        {
            var cacheName = fixture.Create<string>();
            var id = fixture.Create<string>();

            new DistributedCacheKey(cacheName, id).ToString().Should().Be($"{cacheName}:{id}");
        }

        [Test]
        public void Equals_ForTheSameCacheNameAndId_IsTrue()
        {
            var cacheName = fixture.Create<string>();
            var id = fixture.Create<string>();

            new DistributedCacheKey(cacheName, id).Should().Be(new DistributedCacheKey(cacheName, id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_WithNoCacheName_Throws(string cacheName)
        {
            var construct = () => new DistributedCacheKey(cacheName, fixture.Create<string>());

            construct.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Constructor_WithAColonInTheCacheName_Throws()
        {
            var construct = () => new DistributedCacheKey($"{fixture.Create<string>()}:{fixture.Create<string>()}", fixture.Create<string>());

            construct.Should().Throw<ArgumentException>();
        }

        [TestCase(null)]
        [TestCase("")]
        public void Constructor_WithNoId_Throws(string id)
        {
            var construct = () => new DistributedCacheKey(fixture.Create<string>(), id);

            construct.Should().Throw<ArgumentException>();
        }

        /// <summary>The cache name comes first and cannot hold a colon, so an id with colons still makes a unique key.</summary>
        [Test]
        public void Constructor_WithAColonInTheId_Succeeds()
        {
            var construct = () => new DistributedCacheKey(fixture.Create<string>(), $"{fixture.Create<string>()}:{fixture.Create<string>()}");

            construct.Should().NotThrow();
        }
    }
}
