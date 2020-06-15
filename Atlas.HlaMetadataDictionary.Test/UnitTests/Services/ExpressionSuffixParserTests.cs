using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Services;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services
{
    [TestFixture]
    public class ExpressionSuffixParserTests
    {
        [Test]
        public void GetExpressionSuffix_WhenAlleleHasNoSuffix_ReturnsEmptyString()
        {
            const string alleleName = "01:01";
            var suffix = ExpressionSuffixParser.GetExpressionSuffix(alleleName);

            suffix.Should().Be("");
        }
        
        [Test]
        public void GetExpressionSuffix_WhenAlleleHasSuffix_ReturnsSuffix()
        {
            const string alleleName = "01:01N";
            var suffix = ExpressionSuffixParser.GetExpressionSuffix(alleleName);

            suffix.Should().Be("N");
        }
        
        [Test]
        public void GetExpressionSuffix_WhenAlleleHasLowerCaseSuffix_ReturnsEmptyString()
        {
            const string alleleName = "01:01n";
            var suffix = ExpressionSuffixParser.GetExpressionSuffix(alleleName);

            suffix.Should().Be("");
        }
        
        [Test]
        public void IsAlleleNull_WhenAlleleHasNullSuffix_ReturnsTrue()
        {
            const string alleleName = "01:01N";
            var result = ExpressionSuffixParser.IsAlleleNull(alleleName);

            result.Should().BeTrue();
        }
        
        [Test]
        public void IsAlleleNull_WhenAlleleHasNonNullSuffix_ReturnsFalse()
        {
            const string alleleName = "01:01L";
            var result = ExpressionSuffixParser.IsAlleleNull(alleleName);

            result.Should().BeFalse();
        }
        [Test]
        public void IsAlleleNull_WhenAlleleHasNoSuffix_ReturnsFalse()
        {
            const string alleleName = "01:01";
            var result = ExpressionSuffixParser.IsAlleleNull(alleleName);

            result.Should().BeFalse();
        }

        [Test, Repeat(100000), IgnoreExceptOnDevOpsPerfTest("Ran in ~700ms")]
        public void PerfTest()
        {
            ExpressionSuffixParser.GetExpressionSuffix("01:01");
            ExpressionSuffixParser.GetExpressionSuffix("01:01N");
            ExpressionSuffixParser.GetExpressionSuffix("01:01n");
            ExpressionSuffixParser.GetExpressionSuffix("01:01A");
            ExpressionSuffixParser.GetExpressionSuffix("01:01a");
            ExpressionSuffixParser.GetExpressionSuffix("01:01Z");
            ExpressionSuffixParser.GetExpressionSuffix("01:01z");
            ExpressionSuffixParser.GetExpressionSuffix("03:12");
            ExpressionSuffixParser.GetExpressionSuffix("03:12N");
            ExpressionSuffixParser.GetExpressionSuffix("03:12n");
            ExpressionSuffixParser.GetExpressionSuffix("03:12A");
            ExpressionSuffixParser.GetExpressionSuffix("03:12a");
            ExpressionSuffixParser.GetExpressionSuffix("03:12Z");
            ExpressionSuffixParser.GetExpressionSuffix("03:12z");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12N");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12n");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12A");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12a");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12Z");
            ExpressionSuffixParser.GetExpressionSuffix("02:01:02:12z");
        }
    }
}