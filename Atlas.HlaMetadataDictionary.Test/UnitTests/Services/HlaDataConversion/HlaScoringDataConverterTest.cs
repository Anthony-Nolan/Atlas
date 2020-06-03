using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.HlaDataConversion
{
    [TestFixture]
    internal class HlaScoringDataConverterTest :
        MatchedHlaDataConverterTestBase<HlaScoringDataConverter>
    {
        private static readonly List<SerologyEntry> SerologyEntries =
            new List<SerologyEntry> { new SerologyEntry(SerologyName, SeroSubtype, IsDirectMapping) };

        [TestCase("999:999", "999:XX")]
        [TestCase("999:999Q", "999:XX")]
        public override void ConvertToHlaMetadata_WhenTwoFieldExpressingAllele_GeneratesMetadata_ForOriginalNameAndXxCode(
            string alleleName, string xxCodeLookupName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedAllele });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildSingleAlleleMetadata(alleleName),
                BuildXxCodeMetadata(new[] {alleleName}, xxCodeLookupName)
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
                BuildSingleAlleleMetadata(alleleName),
                BuildMultipleAlleleMetadata(nmdpCodeLookupName, new []{alleleName}),
                BuildXxCodeMetadata(new []{alleleName}, xxCodeLookupName)
            };

            if (!string.IsNullOrEmpty(expressionSuffix))
            {
                expectedMetadata.Add(
                    BuildMultipleAlleleMetadata(nmdpCodeLookupName + expressionSuffix, new[] { alleleName }));
            }

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        [TestCase("999:999N")]
        [TestCase("999:999:999N")]
        [TestCase("999:999:999:999N")]
        public override void ConvertToHlaMetadata_WhenNullAllele_GeneratesMetadata_ForOriginalNameOnly(
            string alleleName)
        {
            var matchedAllele = BuildMatchedAllele(alleleName);
            var actualMetadata = MetadataConverter.ConvertToHlaMetadata(new[] { matchedAllele });

            var expectedMetadata = new List<IHlaMetadata>
            {
                BuildSingleAlleleMetadata(alleleName)
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
                BuildSingleAlleleMetadata(alleles[0]),
                BuildSingleAlleleMetadata(alleles[1]),
                BuildSingleAlleleMetadata(alleles[2]),
                BuildMultipleAlleleMetadata(nmdpCodeLookupName, new[]{ alleles[0], alleles[1], alleles[2]}),
                BuildXxCodeMetadata(alleles, xxCodeLookupName)
            };

            actualMetadata.Should().BeEquivalentTo(expectedMetadata);
        }

        protected override IHlaMetadata BuildSerologyHlaMetadata()
        {
            var scoringInfo = new SerologyScoringInfo(SerologyEntries);

            return new HlaScoringMetadata(
                MatchedLocus,
                SerologyName,
                scoringInfo,
                TypingMethod.Serology
            );
        }

        private static IHlaMetadata BuildSingleAlleleMetadata(string alleleName)
        {
            return new HlaScoringMetadata(
                MatchedLocus,
                alleleName,
                BuildSingleAlleleScoringInfoWithMatchingSerologies(alleleName),
                TypingMethod.Molecular
            );
        }

        private static IHlaMetadata BuildMultipleAlleleMetadata(string lookupName, IEnumerable<string> alleleNames)
        {
            return new HlaScoringMetadata(
                MatchedLocus,
                lookupName,
                new MultipleAlleleScoringInfo(
                    alleleNames.Select(BuildSingleAlleleScoringInfoExcludingMatchingSerologies),
                    SerologyEntries),
                TypingMethod.Molecular
            );
        }

        private static IHlaMetadata BuildXxCodeMetadata(IEnumerable<string> alleleNames, string xxCodeLookupName)
        {
            var alleleNamesCollection = alleleNames.ToList();

            return new HlaScoringMetadata(
                MatchedLocus,
                xxCodeLookupName,
                new ConsolidatedMolecularScoringInfo(alleleNamesCollection, alleleNamesCollection, SerologyEntries),
                TypingMethod.Molecular
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
