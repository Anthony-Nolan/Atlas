using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using EnumStringValues;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class LocusHlaMaskerTests
    {
        private ITwoFieldBuilder twoFieldBuilder;
        private IHlaConverter hlaConverter;
        private IMacBuilder macBuilder;
        private IXxCodeBuilder xxCodeBuilder;
        private IHlaDeleter hlaDeleter;

        private ILocusHlaMasker locusHlaMasker;

        [SetUp]
        public void SetUp()
        {
            twoFieldBuilder = Substitute.For<ITwoFieldBuilder>();
            hlaConverter = Substitute.For<IHlaConverter>();
            macBuilder = Substitute.For<IMacBuilder>();
            xxCodeBuilder = Substitute.For<IXxCodeBuilder>();
            hlaDeleter = Substitute.For<IHlaDeleter>();

            locusHlaMasker = new LocusHlaMasker(twoFieldBuilder, hlaConverter, macBuilder, xxCodeBuilder, hlaDeleter);

            twoFieldBuilder.ConvertRandomLocusHlaToTwoField(default).ReturnsForAnyArgs(new TransformationResult
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            hlaConverter.ConvertRandomLocusHla(default, default, default, default).ReturnsForAnyArgs(
                new TransformationResult
                {
                    SelectedTypings = new List<SimulantLocusHla>(),
                    RemainingTypings = new List<SimulantLocusHla>()
                });

            macBuilder.ConvertRandomHlaToMacs(default, default).ReturnsForAnyArgs(new TransformationResult
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(default).ReturnsForAnyArgs(new TransformationResult
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            hlaDeleter.DeleteRandomLocusHla(default).ReturnsForAnyArgs(new TransformationResult
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });
        }

        [Test]
        public async Task MaskHla_MultipleLociTypingsSubmitted_ThrowsException()
        {
            var typings = new List<SimulantLocusHla>
            {
                SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A),
                SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.B)
            };

            var requests = new LocusMaskingRequests
            {
                // important to prevent invoking other exceptions
                TotalSimulantCount = typings.Count
            };

            await locusHlaMasker.Invoking(async x => await x.MaskHlaForSingleLocus(requests, typings))
                .Should().ThrowAsync<Exception>();
        }

        [TestCase(-1)]
        [TestCase(1)]
        public async Task MaskHla_GenotypeCountDoesNotEqualSimulantCount_ThrowsException(int difference)
        {
            var typings = new List<SimulantLocusHla>
            {
                SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A),
                SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A)
            };

            var requests = new LocusMaskingRequests
            {
                TotalSimulantCount = typings.Count + difference
            };

            await locusHlaMasker.Invoking(async x => await x.MaskHlaForSingleLocus(requests, typings))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task MaskHla_NoMaskingRequests_SimulantLocusHlaLeftUnmodified()
        {
            var typing = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build();
            var typings = new List<SimulantLocusHla> {typing};

            var requests = new LocusMaskingRequests {TotalSimulantCount = typings.Count};

            var results = await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);
            var result = results.Single();

            result.Locus.Should().Be(typing.Locus);
            result.GenotypeSimulantId.Should().Be(typing.GenotypeSimulantId);
            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(typing.HlaTyping.Position2);
        }

        [Test]
        public async Task MaskHla_SumOfMaskingProportionsIsZero_SimulantLocusHlaLeftUnmodified()
        {
            var typing = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build();
            var typings = new List<SimulantLocusHla> {typing};

            var categories = EnumExtensions.EnumerateValues<MaskingCategory>().ToList();
            var requests = new LocusMaskingRequests
            {
                TotalSimulantCount = 1,
                MaskingRequests = MaskingRequestBuilder.New.WithCategories(categories).Build(categories.Count)
            };

            var results = await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);
            var result = results.Single();

            result.Locus.Should().Be(typing.Locus);
            result.GenotypeSimulantId.Should().Be(typing.GenotypeSimulantId);
            result.HlaTyping.Position1.Should().Be(typing.HlaTyping.Position1);
            result.HlaTyping.Position2.Should().Be(typing.HlaTyping.Position2);
        }

        [Test]
        public async Task MaskHla_SumOfMaskingProportionsUnder0_ThrowsException()
        {
            var typings = new List<SimulantLocusHla> {SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A)};

            var categories = EnumExtensions.EnumerateValues<MaskingCategory>().ToList();
            var requests = new LocusMaskingRequests
            {
                TotalSimulantCount = typings.Count,
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(-1)
                    .WithCategories(categories)
                    .Build(categories.Count)
            };

            await locusHlaMasker.Invoking(async x => await x.MaskHlaForSingleLocus(requests, typings))
                .Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task MaskHla_SumOfMaskingProportionsOver100_ThrowsException()
        {
            var typings = new List<SimulantLocusHla> {SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A)};

            var categories = EnumExtensions.EnumerateValues<MaskingCategory>().ToList();
            var requests = new LocusMaskingRequests
            {
                TotalSimulantCount = typings.Count,
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(100)
                    .WithCategories(categories)
                    .Build(categories.Count)
            };

            await locusHlaMasker.Invoking(async x => await x.MaskHlaForSingleLocus(requests, typings))
                .Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task MaskHla_TwoFieldRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.TwoField;

            const int maskingProportion = 5;
            const int simulantCount = 100;
            const string hlaVersion = "version";

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(category)
                    .Build(1),
                HlaNomenclatureVersion = hlaVersion,
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await twoFieldBuilder.Received().ConvertRandomLocusHlaToTwoField(
                Arg.Is<TransformationRequest>(x =>
                    x.ProportionToTransform == maskingProportion &&
                    x.TotalSimulantCount == simulantCount));
        }

        [Test]
        public async Task MaskHla_PGroupRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory maskingCategory = MaskingCategory.PGroup;

            const int maskingProportion = 5;
            const int simulantCount = 100;
            const string hlaVersion = "version";
            const ImportTypingCategory importCategory = ImportTypingCategory.LargeGGroup;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(maskingCategory)
                    .Build(1),
                HlaNomenclatureVersion = hlaVersion,
                TypingCategory = importCategory,
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await hlaConverter.Received().ConvertRandomLocusHla(
                Arg.Is<TransformationRequest>(x =>
                    x.ProportionToTransform == maskingProportion &&
                    x.TotalSimulantCount == simulantCount),
                hlaVersion,
                importCategory,
                HlaConversionCategory.PGroup);
        }

        [Test]
        public async Task MaskHla_SerologyRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.Serology;

            const int maskingProportion = 5;
            const int simulantCount = 100;
            const string hlaVersion = "version";
            const ImportTypingCategory importCategory = ImportTypingCategory.LargeGGroup;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(category)
                    .Build(1),
                HlaNomenclatureVersion = hlaVersion,
                TypingCategory = importCategory,
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await hlaConverter.Received().ConvertRandomLocusHla(
                Arg.Is<TransformationRequest>(x =>
                    x.ProportionToTransform == maskingProportion &&
                    x.TotalSimulantCount == simulantCount),
                hlaVersion,
                importCategory,
                HlaConversionCategory.Serology);
        }

        [Test]
        public async Task MaskHla_MacRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.MultipleAlleleCode;

            const int maskingProportion = 5;
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(category)
                    .Build(1),
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await macBuilder.Received().ConvertRandomHlaToMacs(
                Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion && x.TotalSimulantCount == simulantCount),
                Arg.Any<string>());
        }

        [Test]
        public async Task MaskHla_XxCodeRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.XxCode;

            const int maskingProportion = 5;
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(category)
                    .Build(1),
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await xxCodeBuilder.Received().ConvertRandomLocusHlaToXxCodes(Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion &&
                x.TotalSimulantCount == simulantCount));
        }

        [Test]
        public async Task MaskHla_DeleteRequest_DeletesHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.Delete;

            const int maskingProportion = 5;
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                MaskingRequests = MaskingRequestBuilder.New
                    .WithProportion(maskingProportion)
                    .WithCategory(category)
                    .Build(1),
                TotalSimulantCount = simulantCount
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await hlaDeleter.Received().DeleteRandomLocusHla(Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion &&
                x.TotalSimulantCount == simulantCount));
        }

        [Test]
        public async Task MaskHla_OnlyExecutesMaskingRequestsWithProportionValuesGreaterThan0()
        {
            const int maskingProportion = 5;
            const int simulantCount = 100;

            var typings = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build(100).ToList();

            var requests = new LocusMaskingRequests
            {
                TotalSimulantCount = simulantCount,
                MaskingRequests = new List<MaskingRequest>
                {
                    MaskingRequestBuilder.New.WithCategory(MaskingCategory.Delete).Build(),
                    MaskingRequestBuilder.New.WithProportion(maskingProportion).WithCategory(MaskingCategory.XxCode).Build(),
                    MaskingRequestBuilder.New.WithProportion(maskingProportion).WithCategory(MaskingCategory.MultipleAlleleCode).Build()
                }
            };

            await locusHlaMasker.MaskHlaForSingleLocus(requests, typings);

            await hlaDeleter.DidNotReceive().DeleteRandomLocusHla(Arg.Any<TransformationRequest>());

            await xxCodeBuilder.Received(1).ConvertRandomLocusHlaToXxCodes(
                Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion && x.TotalSimulantCount == simulantCount));

            await macBuilder.Received(1).ConvertRandomHlaToMacs(
                Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion && x.TotalSimulantCount == simulantCount), 
                Arg.Any<string>());
        }
    }
}