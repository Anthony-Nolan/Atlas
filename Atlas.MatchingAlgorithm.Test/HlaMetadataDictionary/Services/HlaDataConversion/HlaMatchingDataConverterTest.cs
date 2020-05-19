using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaDataConversion
{
    [TestFixture]
    public class HlaMatchingDataConverterTest :
        MatchedHlaDataConverterTestBase<HlaMatchingDataConverter>
    {
        [TestCase("999:999", "999:XX")]
        [TestCase("999:999Q", "999:XX")]
        public override void ConvertToHlaLookupResults_WhenTwoFieldExpressingAllele_GeneratesLookupResults_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildMolecularHlaLookupResult(alleleName),
                BuildMolecularHlaLookupResult(xxCodeLookupName, new []{alleleName})
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
                BuildMolecularHlaLookupResult(alleleName),
                BuildMolecularHlaLookupResult(nmdpCodeLookupName, new []{alleleName}),
                BuildMolecularHlaLookupResult(xxCodeLookupName, new[] {alleleName})
            };

            if (!string.IsNullOrEmpty(expressionSuffix))
            {
                expectedLookupResults.Add(
                    BuildMolecularHlaLookupResult(nmdpCodeLookupName + expressionSuffix, new[] {alleleName}));
            }

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        [TestCase("999:999N")]
        [TestCase("999:999:999N")]
        [TestCase("999:999:999:999N")]
        public override void ConvertToHlaLookupResults_WhenNullAllele_GeneratesLookupResults_ForOriginalNameOnly(string alleleName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedAllele });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildMolecularHlaLookupResult(alleleName)
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
                BuildMolecularHlaLookupResult(alleles[0]),
                BuildMolecularHlaLookupResult(alleles[1]),
                BuildMolecularHlaLookupResult(alleles[2]),
                BuildMolecularHlaLookupResult(nmdpCodeLookupName, new[] {alleles[0], alleles[1], alleles[2]}),
                BuildMolecularHlaLookupResult(xxCodeLookupName, alleles)
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        /// <summary>
        /// Builds a serology lookup result based on constant values.
        /// </summary>
        protected override IHlaLookupResult BuildSerologyHlaLookupResult()
        {
            return new HlaMatchingLookupResult(
                MatchedLocus,
                SerologyName,
                TypingMethod.Serology,
                new List<string> { PGroupName }
            );
        }

        /// <summary>
        /// Build a molecular lookup result with the allele name used as the Matching P Group by default,
        /// unless a list of 1 or more P Groups is supplied.
        /// </summary>
        private static IHlaLookupResult BuildMolecularHlaLookupResult(string alleleName, IEnumerable<string> pGroups = null)
        {
            return new HlaMatchingLookupResult(
                MatchedLocus,
                alleleName,
                TypingMethod.Molecular,
                pGroups ?? new[] { alleleName }
            );
        }
    }
}