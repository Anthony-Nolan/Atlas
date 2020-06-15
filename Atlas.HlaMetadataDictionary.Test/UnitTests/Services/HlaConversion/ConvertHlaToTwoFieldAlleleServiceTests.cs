using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.MultipleAlleleCodeDictionary;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.HlaConversion
{
    [TestFixture]
    public class ConvertHlaToTwoFieldAlleleServiceTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string DefaultHlaName = "hla";

        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleStringSplitterService alleleStringSplitter;
        private INmdpCodeCache nmdpCodeCache;

        private IConvertHlaToTwoFieldAlleleService converter;

        [SetUp]
        public void SetUp()
        {
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleStringSplitter = Substitute.For<IAlleleStringSplitterService>();
            nmdpCodeCache = Substitute.For<INmdpCodeCache>();

            converter = new ConvertHlaToTwoFieldAlleleService(hlaCategorisationService, alleleStringSplitter, nmdpCodeCache);
        }

        [TestCase("01:01", "01:01")]
        [TestCase("01:01:01", "01:01")]
        [TestCase("01:01:01:01", "01:01")]
        [TestCase("01:01N", "01:01N")]
        [TestCase("01:01:01N", "01:01N")]
        [TestCase("01:01:01:01N", "01:01N")]
        public async Task ConvertHla_HlaIsAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleName(string hlaName, string expectedResult)
        {
            const HlaTypingCategory category = HlaTypingCategory.Allele;
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Include;
            var result = await converter.ConvertHla(DefaultLocus, hlaName, option);

            result.Should().BeEquivalentTo(expectedResult);
        }

        [TestCase("01:01", "01:01")]
        [TestCase("01:01:01", "01:01")]
        [TestCase("01:01:01:01", "01:01")]
        [TestCase("01:01N", "01:01")]
        [TestCase("01:01:01N", "01:01")]
        [TestCase("01:01:01:01N", "01:01")]
        public async Task ConvertHla_HlaIsAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleName(string hlaName, string expectedResult)
        {
            const HlaTypingCategory category = HlaTypingCategory.Allele;
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, hlaName, option);

            result.Should().BeEquivalentTo(expectedResult);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_GetsAlleleNamesFromString(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            await converter.ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixOptions.Include);

            alleleStringSplitter.Received().GetAlleleNamesFromAlleleString(DefaultHlaName);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01", "02:01:01:01" };
            alleleStringSplitter.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleStringWithNullAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01N", "02:01:01:01" };
            alleleStringSplitter.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo("01:01N", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01", "02:01:01:01" };
            alleleStringSplitter.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleStringWithNullAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01N", "02:01:01:01" };
            alleleStringSplitter.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_GetsAllelesForNmdpCode()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            await converter.ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixOptions.Include);

            await nmdpCodeCache.Received().GetOrAddAllelesForNmdpCode(DefaultLocus, DefaultHlaName);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCodeWithNullAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01N", "02:01" };
            nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCodeWithNullAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01N", "02:01" };
            nmdpCodeCache.GetOrAddAllelesForNmdpCode(DefaultLocus, DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixOptions option = ExpressionSuffixOptions.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option);

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        // TODO: ATLAS-367, 368, 369, 370: Add tests when conversion logic for other HLA categories have been implemented
    }
}
