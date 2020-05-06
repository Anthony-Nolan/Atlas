using FluentAssertions;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Services.HlaDataConversion;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Services.HlaDataConversion
{
    [TestFixture]
    public class HlaScoringDataConverterTest :
        MatchedHlaDataConverterTestBase<HlaScoringDataConverter>
    {
        private static readonly List<SerologyEntry> SerologyEntries =
            new List<SerologyEntry> { new SerologyEntry(SerologyName, SeroSubtype, IsDirectMapping) };

        [TestCase("999:999", "999:XX")]
        [TestCase("999:999Q", "999:XX")]
        public override void ConvertToHlaLookupResults_WhenTwoFieldExpressingAllele_GeneratesLookupResults_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildSingleAlleleLookupResult(alleleName),
                BuildXxCodeLookupResult(new[] {alleleName}, xxCodeLookupName)
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [TestCase("999:999:999", "", "999:999", "999:XX")]
        [TestCase("999:999:999:999", "", "999:999", "999:XX")]
        [TestCase("999:999:999L", "L", "999:999", "999:XX")]
        public override void ConvertToHlaLookupResults_WhenThreeOrFourFieldExpressingAllele_GeneratesLookupResults_ForOriginalNameAndNmdpCodeAndXxCode(
            string alleleName, string expressionSuffix, string nmdpCodeLookupName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildSingleAlleleLookupResult(alleleName),
                BuildMultipleAlleleLookupResult(nmdpCodeLookupName, new []{alleleName}),
                BuildXxCodeLookupResult(new []{alleleName}, xxCodeLookupName)
            };

            if (!string.IsNullOrEmpty(expressionSuffix))
            {
                expectedLookupResults.Add(
                    BuildMultipleAlleleLookupResult(nmdpCodeLookupName + expressionSuffix, new[] { alleleName }));
            }

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [TestCase("999:999N")]
        [TestCase("999:999:999N")]
        [TestCase("999:999:999:999N")]
        public override void ConvertToHlaLookupResults_WhenNullAllele_GeneratesLookupResults_ForOriginalNameOnly(
            string alleleName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildSingleAlleleLookupResult(alleleName)
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [Test]
        public override void ConvertToHlaLookupResults_WhenAllelesHaveSameTruncatedNameVariant_GeneratesLookupResult_ForEachUniqueLookupName()
        {
            string[] alleles = { "999:999:998", "999:999:999:01", "999:999:999:02" };
            const string nmdpCodeLookupName = "999:999";
            const string xxCodeLookupName = "999:XX";

            var matchedAlleles = alleles.Select(BuildMatchedAllele).ToList();
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(matchedAlleles);

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildSingleAlleleLookupResult(alleles[0]),
                BuildSingleAlleleLookupResult(alleles[1]),
                BuildSingleAlleleLookupResult(alleles[2]),
                BuildMultipleAlleleLookupResult(nmdpCodeLookupName, new[]{ alleles[0], alleles[1], alleles[2]}),
                BuildXxCodeLookupResult(alleles, xxCodeLookupName)
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        protected override IHlaLookupResult BuildSerologyHlaLookupResult()
        {
            var scoringInfo = new SerologyScoringInfo(SerologyEntries);

            return new HlaScoringLookupResult(
                MatchedLocus,
                SerologyName,
                LookupNameCategory.Serology,
                scoringInfo);
        }

        private static IHlaLookupResult BuildSingleAlleleLookupResult(string alleleName)
        {
            return new HlaScoringLookupResult(
                MatchedLocus,
                alleleName,
                LookupNameCategory.OriginalAllele,
                BuildSingleAlleleScoringInfoWithMatchingSerologies(alleleName)
            );
        }

        private static IHlaLookupResult BuildMultipleAlleleLookupResult(string lookupName, IEnumerable<string> alleleNames)
        {
            return new HlaScoringLookupResult(
                MatchedLocus,
                lookupName,
                LookupNameCategory.NmdpCodeAllele,
                new MultipleAlleleScoringInfo(
                    alleleNames.Select(BuildSingleAlleleScoringInfoExcludingMatchingSerologies),
                    SerologyEntries)
            );
        }

        private static IHlaLookupResult BuildXxCodeLookupResult(IEnumerable<string> alleleNames, string xxCodeLookupName)
        {
            var alleleNamesCollection = alleleNames.ToList();

            return new HlaScoringLookupResult(
                MatchedLocus,
                xxCodeLookupName,
                LookupNameCategory.XxCode,
                new ConsolidatedMolecularScoringInfo(alleleNamesCollection, alleleNamesCollection, SerologyEntries)
            );
        }

        private static SingleAlleleScoringInfo BuildSingleAlleleScoringInfoWithMatchingSerologies(string alleleName)
        {
            return new SingleAlleleScoringInfo(
                alleleName,
                AlleleTypingStatus.GetDefaultStatus(),
                alleleName,
                alleleName,
                SerologyEntries
                );
        }

        private static SingleAlleleScoringInfo BuildSingleAlleleScoringInfoExcludingMatchingSerologies(string alleleName)
        {
            return new SingleAlleleScoringInfo(
                alleleName,
                AlleleTypingStatus.GetDefaultStatus(),
                alleleName,
                alleleName
            );
        }
    }
}
