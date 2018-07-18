using FluentAssertions;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaDataConversion
{
    [TestFixture]
    public class HlaScoringDataConverterTest :
        MatchedHlaDataConverterTestBase<HlaScoringDataConverter>
    {
        private static readonly List<SerologyEntry> SerologyEntries = 
            new List<SerologyEntry>{new SerologyEntry(SerologyName, SeroSubtype)};
        private const string XxCodeLookupName = "999";

        [TestCase("999:999", "")]
        [TestCase("999:999Q", "Q")]
        public void ConvertToHlaLookupResults_WhenTwoFieldAllele_TwoLookupResultsGenerated(
            string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedSingleAlleleLookupResult(alleleName),
                BuildExpectedXxCodeLookupResult(new[] {alleleName})
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
                BuildExpectedSingleAlleleLookupResult(alleleName),
                BuildExpectedMultipleAlleleLookupResult("999:999" + expressionSuffix, new []{alleleName}),
                BuildExpectedXxCodeLookupResult(new []{alleleName})
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [Test]
        public void ConvertToHlaLookupResults_WhenAllelesHaveSameTruncatedNameVariant_OneLookupResultGeneratedPerUniqueLookupName()
        {
            string[] alleles = { "999:999:998", "999:999:999:01", "999:999:999:02", "999:999:999:03N" };
            var matchedAlleles = alleles.Select(BuildMatchedAllele).ToList();
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(matchedAlleles);

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedSingleAlleleLookupResult(alleles[0]),
                BuildExpectedSingleAlleleLookupResult(alleles[1]),
                BuildExpectedSingleAlleleLookupResult(alleles[2]),
                BuildExpectedSingleAlleleLookupResult(alleles[3]),
                BuildExpectedMultipleAlleleLookupResult("999:999", new[]{ alleles[0], alleles[1], alleles[2]}),
                BuildExpectedMultipleAlleleLookupResult("999:999N", new []{alleles[3]}),
                BuildExpectedXxCodeLookupResult(alleles)
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        protected override IHlaLookupResult BuildExpectedSerologyHlaLookupResult()
        {
            var scoringInfo = new SerologyScoringInfo(SeroSubtype, SerologyEntries);

            return new HlaScoringLookupResult(
                MatchedLocus,
                SerologyName,
                TypingMethod.Serology,
                HlaTypingCategory.Serology,
                scoringInfo);
        }

        private static IHlaLookupResult BuildExpectedSingleAlleleLookupResult(string alleleName)
        {
            return new HlaScoringLookupResult(
                MatchedLocus,
                alleleName,
                TypingMethod.Molecular,
                HlaTypingCategory.Allele,
                BuildSingleAlleleScoringInfo(alleleName)
            );
        }

        private static IHlaLookupResult BuildExpectedMultipleAlleleLookupResult(string lookupName, IEnumerable<string> alleleNames)
        {
            return new HlaScoringLookupResult(
                MatchedLocus,
                lookupName,
                TypingMethod.Molecular,
                HlaTypingCategory.Allele,
                new MultipleAlleleScoringInfo(alleleNames.Select(BuildSingleAlleleScoringInfo))
            );
        }

        private static IHlaLookupResult BuildExpectedXxCodeLookupResult(IEnumerable<string> alleleNames)
        {
            var alleleNamesCollection = alleleNames.ToList();

            return new HlaScoringLookupResult(
                MatchedLocus,
                XxCodeLookupName,
                TypingMethod.Molecular,
                HlaTypingCategory.XxCode,
                new XxCodeScoringInfo(alleleNamesCollection, alleleNamesCollection, SerologyEntries)
            );
        }

        private static SingleAlleleScoringInfo BuildSingleAlleleScoringInfo(
            string alleleName)
        {
            return new SingleAlleleScoringInfo(
                alleleName,
                AlleleTypingStatus.GetDefaultStatus(),
                alleleName,
                alleleName,
                SerologyEntries
                );
        }
    }
}
