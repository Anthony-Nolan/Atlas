using FluentAssertions;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Services
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
    }
}