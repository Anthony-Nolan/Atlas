using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.Services;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using static Atlas.Common.GeneticData.Hla.Models.TypingMethod;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MetadataServices
{
    [TestFixture]
    public class LocusHlaMatchingMetadataServiceTest
    {
        private const Locus MatchedLocus = Locus.A;
        private IHlaMatchingMetadataService matchingMetadataService;
        private ILocusHlaMatchingMetadataService locusHlaMatchingMetadataService;

        [SetUp]
        public void LocusHlaMatchingMetadataServiceTest_SetUpBeforeEachTest()
        {
            matchingMetadataService = Substitute.For<IHlaMatchingMetadataService>();
            locusHlaMatchingMetadataService = new LocusHlaMatchingMetadataService(matchingMetadataService);
        }

        [TestCase(Molecular, Molecular)]
        [TestCase(Serology, Serology)]
        [TestCase(Molecular, Serology)]
        [TestCase(Serology, Molecular)]
        public async Task GetHlaMatchingMetadataForLocus_WhenTwoExpressingTypings_ReturnsExpectedMetadata(
            TypingMethod typingMethod1,
            TypingMethod typingMethod2)
        {
            const string hlaString1 = "hla-1";
            const string hlaString2 = "hla-2";
            const string pGroup1 = "p-group-1";
            const string pGroup2 = "p-group-2";

            var metadata1 =
                new HlaMatchingMetadata(MatchedLocus, hlaString1, typingMethod1, new[] { pGroup1 });
            var metadata2 =
                new HlaMatchingMetadata(MatchedLocus, hlaString2, typingMethod2, new[] { pGroup2 });

            matchingMetadataService
                .GetHlaMetadata(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(metadata1, metadata2);

            var actualResults = await locusHlaMatchingMetadataService.GetHlaMatchingMetadata(
                MatchedLocus,
                new LocusInfo<string>(hlaString1, hlaString2), "hla-db-version");

            actualResults.Position1.MatchingPGroups.Should().BeEquivalentTo(pGroup1);
            actualResults.Position1.LookupName.Should().Be(hlaString1);

            actualResults.Position2.MatchingPGroups.Should().BeEquivalentTo(pGroup2);
            actualResults.Position2.LookupName.Should().Be(hlaString2);
        }

        [TestCase(Molecular)]
        [TestCase(Serology)]
        public async Task GetHlaMatchingMetadataForLocus_WhenPositionOneIsExpressedTyping_AndPositionTwoIsNullAllele_PGroupOfOneCopiedToTwo(
            TypingMethod expressedHlaTypingMethod)
        {
            const string expressingPosition1 = "expressed-hla";
            const string nullExpressingPosition2 = "null-allele";
            const string pGroup = "expressed-hla-p-group";

            var expressedHlaResult =
                new HlaMatchingMetadata(MatchedLocus, expressingPosition1, expressedHlaTypingMethod, new[] { pGroup });
            var nullAlleleResult =
                new HlaMatchingMetadata(MatchedLocus, nullExpressingPosition2, Molecular, new string[] { });

            matchingMetadataService
                .GetHlaMetadata(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(expressedHlaResult, nullAlleleResult);

            var actualResults = await locusHlaMatchingMetadataService.GetHlaMatchingMetadata(
                MatchedLocus,
                new LocusInfo<string>(expressingPosition1, nullExpressingPosition2), "hla-db-version");
            var expectedMergedName = NullAlleleHandling.CombineAlleleNames(nullExpressingPosition2, expressingPosition1);

            actualResults.Position1.MatchingPGroups.Should().BeEquivalentTo(pGroup);
            actualResults.Position1.LookupName.Should().Be(expressingPosition1);

            actualResults.Position2.LookupName.Should().Be(expectedMergedName);
            actualResults.Position2.MatchingPGroups.Should().BeEquivalentTo(pGroup);
        }

        [TestCase(Molecular)]
        [TestCase(Serology)]
        public async Task GetHlaMatchingMetadataForLocus_WhenPositionOneIsNullAllele_AndPositionTwoIsExpressedTyping_PGroupOfTwoCopiedToOne(
            TypingMethod expressedHlaTypingMethod)
        {
            const string nullExpressionPosition1 = "null-allele";
            const string expressingPosition2 = "expressed-hla";
            const string pGroup = "expressed-hla-p-group";

            var nullAlleleResult =
                new HlaMatchingMetadata(MatchedLocus, nullExpressionPosition1, Molecular, new string[] { });
            var expressedHlaResult =
                new HlaMatchingMetadata(MatchedLocus, expressingPosition2, expressedHlaTypingMethod, new[] { pGroup });

            matchingMetadataService
                .GetHlaMetadata(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(nullAlleleResult, expressedHlaResult);

            var actualResults = await locusHlaMatchingMetadataService.GetHlaMatchingMetadata(
                MatchedLocus,
                new LocusInfo<string>(nullExpressionPosition1, expressingPosition2),
                "hla-db-version");
            var expectedMergedName = NullAlleleHandling.CombineAlleleNames(nullExpressionPosition1, expressingPosition2);

            actualResults.Position2.MatchingPGroups.Should().BeEquivalentTo(pGroup);
            actualResults.Position2.LookupName.Should().Be(expressingPosition2);

            actualResults.Position1.LookupName.Should().Be(expectedMergedName);
            actualResults.Position1.MatchingPGroups.Should().BeEquivalentTo(pGroup);
        }

        [Test]
        public async Task GetHlaMatchingMetadataForLocus_WhenTwoNEWTypings_ReturnsExpectedMetadata()
        {
            const string hlaString = "NEW";
            var pGroup = new List<string>();

            HlaMatchingMetadata metadata1 = null;
            HlaMatchingMetadata metadata2 = null;

            matchingMetadataService
                .GetHlaMetadata(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(metadata1, metadata2);


            var actualResults = await locusHlaMatchingMetadataService.GetHlaMatchingMetadata(
                MatchedLocus,
                new LocusInfo<string>(hlaString, hlaString), "hla-db-version");

            actualResults.Position1.MatchingPGroups.Should().BeEquivalentTo(pGroup);
            actualResults.Position1.LookupName.Should().Be(hlaString);

            actualResults.Position2.MatchingPGroups.Should().BeEquivalentTo(pGroup);
            actualResults.Position2.LookupName.Should().Be(hlaString);
        }
    }
}