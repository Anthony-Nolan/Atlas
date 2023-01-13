using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
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

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.HlaConversion
{
    [TestFixture]
    public class HlaConverterTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string DefaultHlaName = "hla";

        private IHlaNameToTwoFieldAlleleConverter hlaNameToTwoFieldAlleleConverter;
        private IHlaScoringMetadataService scoringMetadataService;
        private ISmallGGroupMetadataService smallGGroupMetadataService;

        private IHlaConverter hlaConverter;

        [SetUp]
        public void SetUp()
        {
            hlaNameToTwoFieldAlleleConverter = Substitute.For<IHlaNameToTwoFieldAlleleConverter>();
            scoringMetadataService = Substitute.For<IHlaScoringMetadataService>();
            smallGGroupMetadataService = Substitute.For<ISmallGGroupMetadataService>();

            hlaConverter = new HlaConverter(
                hlaNameToTwoFieldAlleleConverter,
                scoringMetadataService,
                smallGGroupMetadataService);
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
            const TargetHlaCategory targetHla = TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix;

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla
            });

            await hlaNameToTwoFieldAlleleConverter.Received()
                .ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixBehaviour.Include, Arg.Any<string>());
        }

        [Test]
        public async Task ConvertHla_TargetIsTwoFieldAlleleExcludingExpressionSuffix_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix;

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla
            });

            await hlaNameToTwoFieldAlleleConverter.Received()
                .ConvertHla(DefaultLocus, DefaultHlaName, ExpressionSuffixBehaviour.Exclude, Arg.Any<string>());
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate GGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsGGroup_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.GGroup;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
                HlaNomenclatureVersion = version
            });

            await scoringMetadataService.Received()
                .GetHlaMetadata(DefaultLocus, DefaultHlaName, version);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate GGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsGGroup_ReturnsMatchingGGroups()
        {
            var gGroups = new List<string> {"g-group1", "g-group-2"};
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingGGroups(gGroups).Build();
            var metadata = BuildHlaScoringMetadata(info);
            scoringMetadataService.GetHlaMetadata(DefaultLocus, DefaultHlaName, Arg.Any<string>()).Returns(metadata);

            const TargetHlaCategory targetHla = TargetHlaCategory.GGroup;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
                HlaNomenclatureVersion = version
            });

            result.Should().BeEquivalentTo(gGroups);
        }

        [Test]
        public async Task ConvertHla_TargetIsSmallGGroup_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.SmallGGroup;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
                HlaNomenclatureVersion = version
            });

            await smallGGroupMetadataService.Received()
                .GetSmallGGroups(DefaultLocus, DefaultHlaName, version);
        }

        [Test]
        public async Task ConvertHla_TargetIsSmallGGroup_ReturnsSmallGGroups()
        {
            var gGroups = new List<string> {"g-group1", "g-group-2"};
            smallGGroupMetadataService.GetSmallGGroups(default, default, default).ReturnsForAnyArgs(gGroups);

            const TargetHlaCategory targetHla = TargetHlaCategory.SmallGGroup;
            const string version = "version";

            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
                HlaNomenclatureVersion = version
            });

            result.Should().BeEquivalentTo(gGroups);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate PGroup lookup service
        [Test]
        public async Task ConvertHla_TargetIsPGroup_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.PGroup;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
                HlaNomenclatureVersion = version
            });

            await scoringMetadataService.Received()
                .GetHlaMetadata(DefaultLocus, DefaultHlaName, version);
        }

        //TODO ATLAS-394: After HMD has been decoupled from Scoring, test using appropriate Serology lookup service
        [Test]
        public async Task ConvertHla_TargetIsSerology_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.Serology;
            const string version = "version";

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
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
            var serologies = new List<SerologyEntry> {new SerologyEntry(serologyName, SerologySubtype.Associated, false)};
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingSerologies(serologies).Build();
            var metadata = BuildHlaScoringMetadata(info);
            scoringMetadataService.GetHlaMetadata(DefaultLocus, DefaultHlaName, Arg.Any<string>()).Returns(metadata);

            const TargetHlaCategory targetHla = TargetHlaCategory.Serology;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
            {
                TargetHlaCategory = targetHla,
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