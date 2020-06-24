using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.MatchedHlaConversion;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MatchedHlaConversion
{
    [TestFixture]
    internal class HlaToMatchingMetaDataConverterTests :
        MatchedHlaToMetaDataConverterTestBase<HlaToMatchingMetaDataConverter>
    {
        [TestCase("999:999", "999:XX")]
        [TestCase("999:999Q", "999:XX")]
        public override void ConvertToHlaMetadata_WhenTwoFieldExpressingAllele_GeneratesMetadata_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedAllele });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildMolecularHlaMetadata(alleleName),
                BuildMolecularHlaMetadata(xxCodeLookupName, new []{alleleName})
            };

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        [TestCase("999:999:999", "", "999:999", "999:XX")]
        [TestCase("999:999:999:999", "", "999:999", "999:XX")]
        [TestCase("999:999:999L", "L", "999:999", "999:XX")]
        public override void ConvertToHlaMetadata_WhenThreeOrFourFieldExpressingAllele_GeneratesMetadata_ForOriginalNameAndNmdpCodeAndXxCode(
            string alleleName, string expressionSuffix, string nmdpCodeLookupName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedAllele });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildMolecularHlaMetadata(alleleName),
                BuildMolecularHlaMetadata(nmdpCodeLookupName, new []{alleleName}),
                BuildMolecularHlaMetadata(xxCodeLookupName, new[] {alleleName})
            };

            if (!string.IsNullOrEmpty(expressionSuffix))
            {
                expectedMetadata.Add(
                    BuildMolecularHlaMetadata(nmdpCodeLookupName + expressionSuffix, new[] {alleleName}));
            }

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        [TestCase("999:999N")]
        [TestCase("999:999:999N")]
        [TestCase("999:999:999:999N")]
        public override void ConvertToHlaMetadata_WhenNullAllele_GeneratesMetadata_ForOriginalNameOnly(string alleleName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedAllele });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildMolecularHlaMetadata(alleleName)
            };

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        [Test]
        public override void ConvertToHlaMetadata_WhenAllelesHaveSameTruncatedNameVariant_GeneratesMetadata_ForEachUniqueLookupName()
        {
            string[] alleles = { "999:999:998", "999:999:999:01", "999:999:999:02" };
            const string nmdpCodeLookupName = "999:999";
            const string xxCodeLookupName = "999:XX";

            var matchedAlleles = alleles.Select(BuildMatchedAllele).ToList();
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(matchedAlleles);

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildMolecularHlaMetadata(alleles[0]),
                BuildMolecularHlaMetadata(alleles[1]),
                BuildMolecularHlaMetadata(alleles[2]),
                BuildMolecularHlaMetadata(nmdpCodeLookupName, new[] {alleles[0], alleles[1], alleles[2]}),
                BuildMolecularHlaMetadata(xxCodeLookupName, alleles)
            };

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        /// <summary>
        /// Builds Serology Metadata based on constant values.
        /// </summary>
        protected override IHlaMetadata BuildSerologyHlaMetadata()
        {
            return new HlaMatchingMetadata(
                MatchedLocus,
                SerologyName,
                TypingMethod.Serology,
                new List<string> { PGroupName }
            );
        }

        /// <summary>
        /// Builds Molecular Metadata with the allele name used as the Matching P Group by default,
        /// unless a list of 1 or more P Groups is supplied.
        /// </summary>
        private static IHlaMetadata BuildMolecularHlaMetadata(string alleleName, IEnumerable<string> pGroups = null)
        {
            return new HlaMatchingMetadata(
                MatchedLocus,
                alleleName,
                TypingMethod.Molecular,
                pGroups ?? new[] { alleleName }
            );
        }
    }
}