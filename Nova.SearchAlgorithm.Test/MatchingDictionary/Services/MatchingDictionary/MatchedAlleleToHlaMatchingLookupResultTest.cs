using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class MatchedAlleleToHlaMatchingLookupResultTest
    {
        [TestCase("01:01:02", "")]
        [TestCase("01:01:01:01", "")]
        [TestCase("01:01:38L", "L")]
        [TestCase("01:01:01:02N", "N")]
        public void MatchedAlleleToHlaMatchingLookupResult_ThreeOrFourFieldAllele_ThreeEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildTestObjectFromAlleleName(alleleName);

            var expected = new List<HlaMatchingLookupResult>
            {
                new HlaMatchingLookupResult(matchedAllele, alleleName),
                new HlaMatchingLookupResult(matchedAllele, "01:01" + expressionSuffix),
                new HlaMatchingLookupResult(matchedAllele, "01")
            };

            TestGenerationOfHlaMatchingLookupResultFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [TestCase("01:32", "")]
        [TestCase("01:248Q", "Q")]
        public void MatchedAlleleToHlaMatchingLookupResult_TwoFieldAllele_TwoEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildTestObjectFromAlleleName(alleleName);

            var expected = new List<HlaMatchingLookupResult>
            {
                new HlaMatchingLookupResult(matchedAllele, alleleName),
                new HlaMatchingLookupResult(matchedAllele, "01")
            };

            TestGenerationOfHlaMatchingLookupResultFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [Test]
        public void MatchedAlleleToHlaMatchingLookupResult_AllelesWithSameTruncatedNames_OneEntryPerNameAndSubtypeCombination()
        {
            string[] alleles = { "01:01:01:01", "01:01:01:03", "01:01:51" };

            var matchedAlleles = alleles.Select(BuildTestObjectFromAlleleName).ToList();

            var expected = new List<HlaMatchingLookupResult>
            {
                new HlaMatchingLookupResult(matchedAlleles[0], alleles[0]),
                new HlaMatchingLookupResult(matchedAlleles[1], alleles[1]),
                new HlaMatchingLookupResult(matchedAlleles[2], alleles[2]),
                new HlaMatchingLookupResult(matchedAlleles[0], "01:01"),
                new HlaMatchingLookupResult(matchedAlleles[0], "01")
            };

            TestGenerationOfHlaMatchingLookupResultFromMatchedAlleles(matchedAlleles, expected);
        }

        private static MatchedAllele BuildTestObjectFromAlleleName(string alleleName)
        {
            const MatchLocus matchLocus = MatchLocus.A;

            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new AlleleTyping(matchLocus, alleleName));

            return new MatchedAllele(infoForMatching, new List<SerologyMappingForAllele>());
        }

        private static void TestGenerationOfHlaMatchingLookupResultFromMatchedAlleles(
            IEnumerable<MatchedAllele> matchedAlleles, IEnumerable<HlaMatchingLookupResult> expected)
        {
            var actual = matchedAlleles.ToHlaMatchingLookupResult();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
