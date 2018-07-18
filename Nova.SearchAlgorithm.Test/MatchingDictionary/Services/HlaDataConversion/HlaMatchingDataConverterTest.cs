using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaDataConversion
{
    [TestFixture]
    public class HlaMatchingDataConverterTest : 
        MatchedHlaDataConverterTestBase<HlaMatchingDataConverter>
    {
        [TestCase("999:999", "")]
        [TestCase("999:999Q", "Q")]
        public void ConvertToHlaLookupResults_WhenTwoFieldAllele_TwoLookupResultsGenerated(
            string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedMolecularHlaLookupResult(alleleName),
                BuildExpectedMolecularHlaLookupResult("999")
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [TestCase("999:999:999", "")]
        [TestCase("999:999:999:999", "")]
        [TestCase("999:999:999L", "L")]
        [TestCase("999:999:999:999N", "N")]
        public void ConvertToHlaLookupResults_WhenThreeOrFourFieldAllele_ThreeLookupResultsGenerated(
            string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedMolecularHlaLookupResult(alleleName),
                BuildExpectedMolecularHlaLookupResult("999:999" + expressionSuffix),
                BuildExpectedMolecularHlaLookupResult("999")
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [Test]
        public void ConvertToHlaLookupResults_WhenAllelesHaveSameTruncatedNameVariant_OneEntryGeneratedPerUniqueNameVariant()
        {
            string[] alleles = { "999:999:998", "999:999:999:01", "999:999:999:02", "999:999:999:03N" };
            var matchedAlleles = alleles.Select(BuildMatchedAllele).ToList();
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(matchedAlleles);

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedMolecularHlaLookupResult(alleles[0]),
                BuildExpectedMolecularHlaLookupResult(alleles[1]),
                BuildExpectedMolecularHlaLookupResult(alleles[2]),
                BuildExpectedMolecularHlaLookupResult(alleles[3]),
                BuildExpectedMolecularHlaLookupResult("999:999"),
                BuildExpectedMolecularHlaLookupResult("999:999N"),
                BuildExpectedMolecularHlaLookupResult("999")
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        protected override IHlaLookupResult BuildExpectedSerologyHlaLookupResult()
        {
            return new HlaMatchingLookupResult(
                MatchedLocus,
                SerologyName,
                TypingMethod.Serology,
                new List<string>()
            );
        }

        private static IHlaLookupResult BuildExpectedMolecularHlaLookupResult(string alleleName)
        {
            return new HlaMatchingLookupResult(
                MatchedLocus,
                alleleName,
                TypingMethod.Molecular,
                new List<string>()
                );
        }
    }
}
