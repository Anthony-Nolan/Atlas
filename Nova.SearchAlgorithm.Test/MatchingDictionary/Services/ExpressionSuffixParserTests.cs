using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services
{
    [TestFixture]
    public class ExpressionSuffixParserTests
    {
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