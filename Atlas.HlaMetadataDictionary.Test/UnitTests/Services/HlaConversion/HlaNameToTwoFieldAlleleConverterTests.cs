using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.HlaConversion
{
    [TestFixture]
    public class HlaNameToTwoFieldAlleleConverterTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string DefaultHlaName = "hla";

        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleNamesExtractor alleleNamesExtractor;
        private IMacDictionary macDictionary;
        private IAlleleGroupExpander groupExpander;

        private IHlaNameToTwoFieldAlleleConverter converter;

        [SetUp]
        public void SetUp()
        {
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleNamesExtractor = Substitute.For<IAlleleNamesExtractor>();
            macDictionary = Substitute.For<IMacDictionary>();
            groupExpander = Substitute.For<IAlleleGroupExpander>();

            converter = new HlaNameToTwoFieldAlleleConverter(
                hlaCategorisationService, alleleNamesExtractor, macDictionary, groupExpander);
        }

        [TestCase("01:01", "01:01")]
        [TestCase("01:01:01", "01:01")]
        [TestCase("01:01:01:01", "01:01")]
        [TestCase("01:01N", "01:01N")]
        [TestCase("01:01:01N", "01:01N")]
        [TestCase("01:01:01:01N", "01:01N")]
        [TestCase("01:01:01:01A", "01:01A")]
        [TestCase("01:01:01:01C", "01:01C")]
        [TestCase("01:01:01:01L", "01:01L")]
        [TestCase("01:01:01:01Q", "01:01Q")]
        [TestCase("01:01:01:01S", "01:01S")]
        public async Task ConvertHla_HlaIsAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleName(string hlaName, string expectedResult)
        {
            const HlaTypingCategory category = HlaTypingCategory.Allele;
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, hlaName, option, "version");

            result.Should().BeEquivalentTo(expectedResult);
        }

        [TestCase("01:01", "01:01")]
        [TestCase("01:01:01", "01:01")]
        [TestCase("01:01:01:01", "01:01")]
        [TestCase("01:01N", "01:01")]
        [TestCase("01:01:01N", "01:01")]
        [TestCase("01:01:01:01N", "01:01")]
        [TestCase("01:01:01:01A", "01:01")]
        [TestCase("01:01:01:01C", "01:01")]
        [TestCase("01:01:01:01L", "01:01")]
        [TestCase("01:01:01:01Q", "01:01")]
        [TestCase("01:01:01:01S", "01:01")]
        public async Task ConvertHla_HlaIsAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleName(string hlaName, string expectedResult)
        {
            const HlaTypingCategory category = HlaTypingCategory.Allele;
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, hlaName, option, "version");

            result.Should().BeEquivalentTo(expectedResult);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public async Task ConvertHla_HlaIsAlleleGroup_ExpandsGroup(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            await converter.ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixBehaviour.Include, "version");

            await groupExpander.Received().ExpandAlleleGroup(Arg.Any<Locus>(), DefaultHlaName, Arg.Any<string>());
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public async Task ConvertHla_HlaIsAlleleGroup_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01:01", "01:50:01:01" };
            groupExpander.ExpandAlleleGroup(default, default, default).ReturnsForAnyArgs(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "01:50");
        }

        [Test]
        public async Task ConvertHla_HlaIsGGroupWithNullAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames()
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(HlaTypingCategory.GGroup);

            var alleleNames = new[] { "01:01:01:01N", "01:50:01:01" };
            groupExpander.ExpandAlleleGroup(default, default, default).ReturnsForAnyArgs(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01N", "01:50");
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public async Task ConvertHla_HlaIsAlleleGroup_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01:01", "01:50:01:01" };
            groupExpander.ExpandAlleleGroup(default, default, default).ReturnsForAnyArgs(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "01:50");
        }

        [Test]
        public async Task ConvertHla_HlaIsGGroupWithNullAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames()
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(HlaTypingCategory.GGroup);

            var alleleNames = new[] { "01:01:01:01N", "01:50:01:01" };
            groupExpander.ExpandAlleleGroup(default, default, default).ReturnsForAnyArgs(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "01:50");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_GetsAlleleNamesFromString(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            await converter.ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixBehaviour.Include, "version");

            alleleNamesExtractor.Received().GetAlleleNamesFromAlleleString(DefaultHlaName);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01", "02:01:01:01" };
            alleleNamesExtractor.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleStringWithNullAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01N", "02:01:01:01" };
            alleleNamesExtractor.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01N", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleString_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01", "02:01:01:01" };
            alleleNamesExtractor.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        public async Task ConvertHla_HlaIsAlleleStringWithNullAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames(HlaTypingCategory category)
        {
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01:01N", "02:01:01:01" };
            alleleNamesExtractor.GetAlleleNamesFromAlleleString(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "02:01");
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_GetsAllelesForNmdpCode()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            await converter.ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixBehaviour.Include, "version");

            await macDictionary.Received().GetHlaFromMac(DefaultHlaName);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            macDictionary.GetHlaFromMac(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCodeWithNullAllele_AndExpressionSuffixShouldBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            macDictionary.GetHlaFromMac(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Include;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCode_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            macDictionary.GetHlaFromMac(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo(alleleNames);
        }

        [Test]
        public async Task ConvertHla_HlaIsNmdpCodeWithNullAllele_AndExpressionSuffixShouldNotBeIncluded_ReturnsExpectedAlleleNames()
        {
            const HlaTypingCategory category = HlaTypingCategory.NmdpCode;
            hlaCategorisationService.GetHlaTypingCategory(DefaultHlaName).Returns(category);

            var alleleNames = new[] { "01:01", "02:01" };
            macDictionary.GetHlaFromMac(DefaultHlaName).Returns(alleleNames);

            const ExpressionSuffixBehaviour option = ExpressionSuffixBehaviour.Exclude;
            var result = await converter.ConvertHla(DefaultLocus, DefaultHlaName, option, "version");

            result.Should().BeEquivalentTo("01:01", "02:01");
        }
    }
}
