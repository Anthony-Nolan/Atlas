using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Services.HlaConversion;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
                smallGGroupMetadataService
            );
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task ConvertHla_HlaNameIsNullOrEmpty_ExceptionThrown(string hlaName)
        {
            await hlaConverter.Invoking(provider => provider.ConvertHla(DefaultLocus, hlaName, new HlaConversionBehaviour()))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task ConvertHla_ConversionBehaviourIsNull_ExceptionThrown()
        {
            await hlaConverter.Invoking(provider => provider.ConvertHla(DefaultLocus, "hla", null))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task ConvertHla_TargetIsTwoFieldAlleleIncludingExpressionSuffix_CallsCorrectConverter()
        {
            const TargetHlaCategory targetHla = TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix;

            await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla
                }
            );

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
                }
            );

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
                }
            );

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

            const TargetHlaCategory targetHla = TargetHlaCategory.GGroup;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla,
                    HlaNomenclatureVersion = version
                }
            );

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
                }
            );

            await smallGGroupMetadataService.Received()
                .GetSmallGGroups(DefaultLocus, DefaultHlaName, version);
        }

        [Test]
        public async Task ConvertHla_TargetIsSmallGGroup_ReturnsSmallGGroups()
        {
            var gGroups = new List<string> { "g-group1", "g-group-2" };
            smallGGroupMetadataService.GetSmallGGroups(default, default, default).ReturnsForAnyArgs(gGroups);

            const TargetHlaCategory targetHla = TargetHlaCategory.SmallGGroup;
            const string version = "version";

            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla,
                    HlaNomenclatureVersion = version
                }
            );

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
                }
            );

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
                }
            );

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

            const TargetHlaCategory targetHla = TargetHlaCategory.Serology;
            const string version = "version";
            var result = await hlaConverter.ConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla,
                    HlaNomenclatureVersion = version
                }
            );

            result.Should().BeEquivalentTo(serologyName);
        }

        #region TryConvertHla

        [TestCase(null)]
        [TestCase("")]
        public async Task TryConvertHla_HlaNameIsNullOrEmpty_ExceptionThrown(string hlaName)
        {
            // A missing argument is still a programming fault, not a name with no data. Parity with ConvertHla.
            await hlaConverter.Invoking(provider => provider.TryConvertHla(DefaultLocus, hlaName, new HlaConversionBehaviour()))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [TestCase(TargetHlaCategory.GGroup)]
        [TestCase(TargetHlaCategory.PGroup)]
        [TestCase(TargetHlaCategory.SmallGGroup)]
        public async Task TryConvertHla_WhenTheNameHasNoData_ReportsNotFoundWithoutTakingTheThrowingPath(TargetHlaCategory targetHla)
        {
            // The three categories match prediction converts to, which is why they are the three with a non-throwing route
            ArrangeNoDataForTheName();

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla
                }
            );

            wasFound.Should().BeFalse();
            hla.Should().BeNull();

            await scoringMetadataService.DidNotReceiveWithAnyArgs().GetHlaMetadata(default, default, default);
            await smallGGroupMetadataService.DidNotReceiveWithAnyArgs().GetSmallGGroups(default, default, default);
        }

        [Test]
        public async Task TryConvertHla_TargetIsGGroup_ReturnsMatchingGGroups()
        {
            var gGroups = new List<string> { "g-group-1", "g-group-2" };
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingGGroups(gGroups).Build();
            scoringMetadataService.TryGetHlaMetadata(default, default, default)
                .ReturnsForAnyArgs((true, BuildHlaScoringMetadata(info)));

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = TargetHlaCategory.GGroup
                }
            );

            wasFound.Should().BeTrue();
            hla.Should().BeEquivalentTo(gGroups);
        }

        [Test]
        public async Task TryConvertHla_TargetIsPGroup_ReturnsMatchingPGroups()
        {
            var pGroups = new List<string> { "p-group-1", "p-group-2" };
            var info = new ConsolidatedMolecularScoringInfoBuilder().WithMatchingPGroups(pGroups).Build();
            scoringMetadataService.TryGetHlaMetadata(default, default, default)
                .ReturnsForAnyArgs((true, BuildHlaScoringMetadata(info)));

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = TargetHlaCategory.PGroup
                }
            );

            wasFound.Should().BeTrue();
            hla.Should().BeEquivalentTo(pGroups);
        }

        [Test]
        public async Task TryConvertHla_TargetIsSmallGGroup_ReturnsSmallGGroups()
        {
            var smallGGroups = new List<string> { "small-g-group-1", "small-g-group-2" };
            smallGGroupMetadataService.TryGetSmallGGroups(default, default, default).ReturnsForAnyArgs((true, smallGGroups));

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = TargetHlaCategory.SmallGGroup
                }
            );

            wasFound.Should().BeTrue();
            hla.Should().BeEquivalentTo(smallGGroups);
        }

        [Test]
        public async Task TryConvertHla_ForANewAllele_ReportsFoundWithNoGroups()
        {
            // The NEW short-circuit of the throwing path, kept: a new allele is a known answer with no groups, and it
            // must not reach a lookup service or be reported as a name with no data.
            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, "NEW", new HlaConversionBehaviour
                {
                    TargetHlaCategory = TargetHlaCategory.GGroup
                }
            );

            wasFound.Should().BeTrue();
            hla.Should().BeEmpty();

            await scoringMetadataService.DidNotReceiveWithAnyArgs().TryGetHlaMetadata(default, default, default);
        }

        [Test]
        public async Task TryConvertHla_TargetHasNoNonThrowingRoute_AndTheNameHasNoData_ReportsNotFound()
        {
            // Serology keeps the throwing path behind a catch: same answer, and its failures are too rare to be worth
            // widening the surface for.
            scoringMetadataService.GetHlaMetadata(default, default, default).ThrowsForAnyArgs(
                new HlaMetadataDictionaryException(DefaultLocus, DefaultHlaName, $"Failed to lookup '{DefaultHlaName}'.")
            );

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = TargetHlaCategory.Serology
                }
            );

            wasFound.Should().BeFalse();
            hla.Should().BeNull();
        }

        [TestCase(TargetHlaCategory.GGroup)]
        [TestCase(TargetHlaCategory.PGroup)]
        [TestCase(TargetHlaCategory.SmallGGroup)]
        [TestCase(TargetHlaCategory.Serology)]
        public async Task TryConvertHla_WhenTheLookupFaults_PropagatesInsteadOfReportingNoData(TargetHlaCategory targetHla)
        {
            // The boundary this method exists to draw, and it has to hold on every route: a name with no data is an
            // answer, a failed storage request is not. Swallowing the second as the first would convert an incomplete
            // expansion into a prediction, silently.
            //
            // A raw TimeoutException is what the metadata services now emit for this, because GetMetadata no longer
            // re-labels non-lookup failures as HlaMetadataDictionaryException. While it did, this arrangement was
            // arranging a fault production could not produce, and the Serology case below proved nothing.
            var fault = new TimeoutException("storage is having a moment");

            scoringMetadataService.TryGetHlaMetadata(default, default, default).ThrowsForAnyArgs(fault);
            scoringMetadataService.GetHlaMetadata(default, default, default).ThrowsForAnyArgs(fault);
            smallGGroupMetadataService.TryGetSmallGGroups(default, default, default).ThrowsForAnyArgs(fault);

            await hlaConverter.Invoking(provider => provider.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                        {
                            TargetHlaCategory = targetHla
                        }
                    )
                )
                .Should().ThrowAsync<TimeoutException>();
        }

        [TestCase(TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix)]
        [TestCase(TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix)]
        public async Task TryConvertHla_WhenTheTwoFieldConversionFaults_PropagatesInsteadOfReportingNoData(TargetHlaCategory targetHla)
        {
            // Its own test rather than another case above: the two-field categories share the fallback `catch` with
            // Serology but not the service behind it, so arranging the scoring service would leave them untouched
            // and pass for the wrong reason.
            hlaNameToTwoFieldAlleleConverter.ConvertHla(default, default, default, default)
                .ThrowsForAnyArgs(new TimeoutException("storage is having a moment"));

            await hlaConverter.Invoking(provider => provider.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                        {
                            TargetHlaCategory = targetHla
                        }
                    )
                )
                .Should().ThrowAsync<TimeoutException>();
        }

        [TestCase(TargetHlaCategory.TwoFieldAlleleIncludingExpressionSuffix)]
        [TestCase(TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix)]
        public async Task TryConvertHla_WhenTheTwoFieldNameHasNoData_ReportsNotFound(TargetHlaCategory targetHla)
        {
            // The other half of the same boundary. Without it, the propagation test above would still pass with the
            // fallback `catch` deleted outright.
            hlaNameToTwoFieldAlleleConverter.ConvertHla(default, default, default, default).ThrowsForAnyArgs(
                new HlaMetadataDictionaryException(DefaultLocus, DefaultHlaName, $"Failed to lookup '{DefaultHlaName}'.")
            );

            var (wasFound, hla) = await hlaConverter.TryConvertHla(DefaultLocus, DefaultHlaName, new HlaConversionBehaviour
                {
                    TargetHlaCategory = targetHla
                }
            );

            wasFound.Should().BeFalse();
            hla.Should().BeNull();
        }

        private void ArrangeNoDataForTheName()
        {
            scoringMetadataService.TryGetHlaMetadata(default, default, default)
                .ReturnsForAnyArgs((false, (IHlaScoringMetadata)null));
            smallGGroupMetadataService.TryGetSmallGGroups(default, default, default)
                .ReturnsForAnyArgs((false, (IEnumerable<string>)null));
        }

        #endregion

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