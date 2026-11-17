using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    /// <summary>ATL-526 experiment: the donor ID range setting.</summary>
    [TestFixture]
    public class DonorIdRangeExperimentTests
    {
        [TearDown]
        public void TearDown()
        {
            DonorIdRangeExperiment.RangeOverride = null;
        }

        [TestCase("1-5500000", 1, 5_500_000)]
        [TestCase(" 2000000 - 4999999 ", 2_000_000, 4_999_999)]
        [TestCase("7-7", 7, 7)]
        public void Parse_WithMinAndMax_ReturnsClosedRange(string value, int min, int max)
        {
            DonorIdRangeExperiment.Parse(value).Should().Be(new DonorIdRange(min, max));
        }

        [Test]
        public void Parse_WithMinOnly_ReturnsRangeWithNoUpperEnd()
        {
            DonorIdRangeExperiment.Parse("38000000-").Should().Be(new DonorIdRange(38_000_000, null));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("5000")]
        [TestCase("-5000")]
        [TestCase("10-5")]
        [TestCase("a-b")]
        [TestCase("1-2-3")]
        [TestCase("1-99999999999")]
        public void Parse_WithInvalidValue_ReturnsNull(string value)
        {
            DonorIdRangeExperiment.Parse(value).Should().BeNull();
        }

        [Test]
        public void SqlCondition_WithClosedRange_UsesBetween()
        {
            new DonorIdRange(10, 20).SqlCondition("m.DonorId").Should().Be("m.DonorId BETWEEN 10 AND 20");
        }

        [Test]
        public void SqlCondition_WithNoUpperEnd_UsesGreaterOrEqual()
        {
            new DonorIdRange(10, null).SqlCondition("m.DonorId").Should().Be("m.DonorId >= 10");
        }

        [Test]
        public void Current_WithOverride_ReturnsParsedOverride()
        {
            DonorIdRangeExperiment.RangeOverride = "100-200";

            DonorIdRangeExperiment.Current.Should().Be(new DonorIdRange(100, 200));
        }

        [Test]
        public void ToString_ReturnsSettingFormat()
        {
            new DonorIdRange(1, 5).ToString().Should().Be("1-5");
            new DonorIdRange(1, null).ToString().Should().Be("1-");
        }
    }
}
