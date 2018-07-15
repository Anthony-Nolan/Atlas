using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public abstract class HlaLookupResultGeneratorTestBase<TGenerator>
        where TGenerator : IHlaLookupResultGenerator, new()
    {
        private TGenerator lookupResultGenerator;
        private const MatchLocus MatchedLocus = MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupResultGenerator = new TGenerator();
        }

        [TestCase("999:999:999", "")]
        [TestCase("999:999:999:999", "")]
        [TestCase("999:999:999L", "L")]
        [TestCase("999:999:999:999N", "N")]
        public void GetHlaLookupResultsFromMatchedAlleles_WhenThreeOrFourFieldAllele_ThreeEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            
            var expected = new List<IHlaLookupResult>
            {
                BuildExpectedHlaLookupResult(matchedAllele, alleleName),
                BuildExpectedHlaLookupResult(matchedAllele, "999:999" + expressionSuffix),
                BuildExpectedHlaLookupResult(matchedAllele, "999")
            };

            TestGenerationOfHlaLookupResultsFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [TestCase("999:999", "")]
        [TestCase("999:999Q", "Q")]
        public void GetHlaLookupResultsFromMatchedAlleles_WhenTwoFieldAllele_TwoEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);

            var expected = new List<IHlaLookupResult>
            {
               BuildExpectedHlaLookupResult(matchedAllele, alleleName),
               BuildExpectedHlaLookupResult(matchedAllele, "999")
            };

            TestGenerationOfHlaLookupResultsFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [Test]
        public void GetHlaLookupResultsFromMatchedAlleles_WhenAllelesHaveSameTruncatedNameVariant_OneEntryGeneratedPerUniqueNameVariant()
        {
            string[] alleles = { "999:999:998", "999:999:999:01", "999:999:999:02" };

            var matchedAlleles = alleles.Select(BuildMatchedAllele).ToList();

            var expected = new List<IHlaLookupResult>
            {
                BuildExpectedHlaLookupResult(matchedAlleles[0], alleles[0]),
                BuildExpectedHlaLookupResult(matchedAlleles[1], alleles[1]),
                BuildExpectedHlaLookupResult(matchedAlleles[2], alleles[2]),
                BuildExpectedHlaLookupResult(matchedAlleles[0], "999:999"),
                BuildExpectedHlaLookupResult(matchedAlleles[0], "999")
            };

            TestGenerationOfHlaLookupResultsFromMatchedAlleles(matchedAlleles, expected);
        }

        protected abstract IHlaLookupResult BuildExpectedHlaLookupResult(MatchedAllele matchedAllele, string alleleName);

        private static MatchedAllele BuildMatchedAllele(string alleleName)
        {
            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new AlleleTyping(MatchedLocus, alleleName));

            return new MatchedAllele(infoForMatching, new List<SerologyMappingForAllele>());
        }

        private void TestGenerationOfHlaLookupResultsFromMatchedAlleles(
            IEnumerable<MatchedAllele> matchedAlleles, IEnumerable<IHlaLookupResult> expected)
        {
            var actual = lookupResultGenerator.GetHlaMatchingLookupResults(matchedAlleles);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
