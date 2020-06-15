using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.HlaConversion
{
    [TestFixture]
    public class HlaConverterTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string DefaultHlaName = "hla";

        private IConvertHlaToTwoFieldAlleleService convertHlaToTwoFieldAlleleService;
        private IHlaScoringMetadataService scoringMetadataService;

        private IHlaConverter hlaConverter;

        [SetUp]
        public void SetUp()
        {
            convertHlaToTwoFieldAlleleService = Substitute.For<IConvertHlaToTwoFieldAlleleService>();
            scoringMetadataService = Substitute.For<IHlaScoringMetadataService>();

            hlaConverter = new HlaConverter(convertHlaToTwoFieldAlleleService, scoringMetadataService);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ConvertHla_HlaNameIsNullOrEmpty_ExceptionThrown(string hlaName)
        {
            hlaConverter.Invoking(async provider => await provider.ConvertHla(DefaultLocus, hlaName, new HlaConversionBehaviour()))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConvertHla_ConversionBehaviourIsNull_ExceptionThrown()
        {
            hlaConverter.Invoking(async provider => await provider.ConvertHla(DefaultLocus, "hla", null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task ConvertHla_TargetIsTwoFieldAlleleIncludingExpressionSuffix_CallsCorrectConverter()
        {
            const TargetHlaOptions targetHla = TargetHlaOptions.TwoFieldAlleleIncludingExpressionSuffix;

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla
            });

            await convertHlaToTwoFieldAlleleService.Received()
                .ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixOptions.Include);
        }

        [Test]
        public async Task ConvertHla_TargetIsTwoFieldAlleleExcludingExpressionSuffix_CallsCorrectConverter()
        {
            const TargetHlaOptions targetHla = TargetHlaOptions.TwoFieldAlleleExcludingExpressionSuffix;

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla
            });

            await convertHlaToTwoFieldAlleleService.Received()
                .ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixOptions.Exclude);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate GGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsGGroup_CallsCorrectConverter()
        {
            const TargetHlaOptions targetHla = TargetHlaOptions.GGroup;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            await scoringMetadataService.Received()
                .GetHlaMetadata(DefaultLocus, DefaultHlaName, version);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate GGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsGGroup_ReturnsMatchingGGroups()
        {
            var gGroups = new List<string> { "g-group1", "g-group-2" };
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingGGroups(gGroups).Build();
            var metadata = BuildHlaScoringMetadata(info);
            scoringMetadataService.GetHlaMetadata(DefaultLocus, DefaultHlaName, Arg.Any<string>()).Returns(metadata);

            const TargetHlaOptions targetHla = TargetHlaOptions.GGroup;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            result.Should().BeEquivalentTo(gGroups);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate PGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsPGroup_CallsCorrectConverter()
        {
            const TargetHlaOptions targetHla = TargetHlaOptions.PGroup;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            await scoringMetadataService.Received()
                .GetHlaMetadata(DefaultLocus, DefaultHlaName, version);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate PGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsPGroup_ReturnsMatchingPGroups()
        {
            var pGroups = new List<string> { "p-group1", "p-group-2" };
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingPGroups(pGroups).Build();
            var metadata = BuildHlaScoringMetadata(info);
            scoringMetadataService.GetHlaMetadata(DefaultLocus, DefaultHlaName, Arg.Any<string>()).Returns(metadata);

            const TargetHlaOptions targetHla = TargetHlaOptions.PGroup;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            result.Should().BeEquivalentTo(pGroups);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate Serology lookup service
        [Test]
        public async Task ConvertHla_TargetIsSerology_CallsCorrectConverter()
        {
            const TargetHlaOptions targetHla = TargetHlaOptions.Serology;
            const string version = "version";
            
            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            await scoringMetadataService.Received()
                .GetHlaMetadata(DefaultLocus, DefaultHlaName, version);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate Serology lookup service
        [Test]
        public async Task ConvertHla_TargetIsSerology_ReturnsMatchingSerologies()
        {
            const string serologyName = "serology";
            var serologies = new List<SerologyEntry> { new SerologyEntry(serologyName, SerologySubtype.Associated, false) };
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingSerologies(serologies).Build();
            var metadata = BuildHlaScoringMetadata(info);
            scoringMetadataService.GetHlaMetadata(DefaultLocus, DefaultHlaName, Arg.Any<string>()).Returns(metadata);

            const TargetHlaOptions targetHla = TargetHlaOptions.Serology;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaOptions = targetHla,
                HlaNomenclatureVersion = version
            });

            result.Should().BeEquivalentTo(serologyName);
        }

        private static IHlaScoringMetadata BuildHlaScoringMetadata(IHlaScoringInfo scoringInfo)
        {
            return new HlaScoringMetadataBuilder()
                .AtLocus(DefaultLocus)
                .WithLookupName(DefaultHlaName)
                .WithHlaScoringInfo(scoringInfo)
                .Build();
        }
    }
}
