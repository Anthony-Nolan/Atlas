using System.Linq;
using Atlas.MatchPrediction.Test.Verification.Services;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class RandomNumberPairGeneratorTests
    {
        private IRandomNumberPairGenerator generator;

        [SetUp]
        public void SetUp()
        {
            generator = new RandomNumberPairGenerator();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1000)]
        public void GenerateRandomNumberPairs_ReturnsRequestedNumberOfPairs(int pairCount)
        {
            var results = generator.GenerateRandomNumberPairs(pairCount, 12345);

            results.Count.Should().Be(pairCount);
        }

        [Test, Repeat(100)]
        public void GenerateRandomNumberPairs_OnlyReturnsNumbersWithinRequestedRange()
        {
            const int max = 999;
            var fullRangeOffNumbers = Enumerable.Range(0, max).ToList();

            var results = generator.GenerateRandomNumberPairs(100, max).ToList();

            results.Select(r => r.Item1).Should().BeSubsetOf(fullRangeOffNumbers);
            results.Select(r => r.Item2).Should().BeSubsetOf(fullRangeOffNumbers);
        }
    }
}
