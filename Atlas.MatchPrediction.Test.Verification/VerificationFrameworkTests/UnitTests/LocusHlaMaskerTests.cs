using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using EnumStringValues;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class LocusHlaMaskerTests
    {
        private IHlaDeleter hlaDeleter;
        private IHlaConverter hlaConverter;
        private IMacBuilder macBuilder;
        private IXxCodeBuilder xxCodeBuilder;

        private ILocusHlaMasker locusHlaMasker;

        [SetUp]
        public void SetUp()
        {
            hlaDeleter = Substitute.For<IHlaDeleter>();
            hlaConverter = Substitute.For<IHlaConverter>();
            macBuilder = Substitute.For<IMacBuilder>();
            xxCodeBuilder = Substitute.For<IXxCodeBuilder>();
            
            locusHlaMasker = new LocusHlaMasker(hlaDeleter, hlaConverter, macBuilder, xxCodeBuilder);

            hlaDeleter.DeleteRandomLocusHla(default).ReturnsForAnyArgs(new TransformationResult()
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            hlaConverter.ConvertRandomLocusHla(default, default, default).ReturnsForAnyArgs(new TransformationResult()
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            macBuilder.ConvertRandomLocusHlaToMacs(default, default).ReturnsForAnyArgs(new TransformationResult()
            {
                SelectedTypings = new List<SimulantLocusHla>(),
                RemainingTypings = new List<SimulantLocusHla>()
            });

            xxCodeBuilder.ConvertRandomLocusHlaToXxCodes(default).ReturnsForAnyArgs(new TransformationResult()
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

            await locusHlaMasker.Invoking(async x => await x.MaskHla(requests, typings))
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

            await locusHlaMasker.Invoking(async x => await x.MaskHla(requests, typings))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task MaskHla_NoMaskingRequests_SimulantLocusHlaLeftUnmodified()
        {
            var typing = SimulantLocusHlaBuilder.New.WithTypingFromLocusName(Locus.A).Build();
            var typings = new List<SimulantLocusHla> {typing};

            var requests = new LocusMaskingRequests {TotalSimulantCount = typings.Count};

            var results = await locusHlaMasker.MaskHla(requests, typings);
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

            var results = await locusHlaMasker.MaskHla(requests, typings);
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

            await locusHlaMasker.Invoking(async x => await x.MaskHla(requests, typings))
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

            await locusHlaMasker.Invoking(async x => await x.MaskHla(requests, typings))
                .Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task MaskHla_PGroupRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.PGroup;

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

            await locusHlaMasker.MaskHla(requests, typings);

            await hlaConverter.Received().ConvertRandomLocusHla(
                Arg.Is<TransformationRequest>(x =>
                    x.ProportionToTransform == maskingProportion &&
                    x.TotalSimulantCount == simulantCount),
                hlaVersion,
                TargetHlaCategory.PGroup);
        }

        [Test]
        public async Task MaskHla_SerologyRequest_ConvertsHlaToRequestedProportions()
        {
            const MaskingCategory category = MaskingCategory.Serology;

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

            await locusHlaMasker.MaskHla(requests, typings);

            await hlaConverter.Received().ConvertRandomLocusHla(
                Arg.Is<TransformationRequest>(x =>
                    x.ProportionToTransform == maskingProportion &&
                    x.TotalSimulantCount == simulantCount),
                hlaVersion,
                TargetHlaCategory.Serology);
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

            await locusHlaMasker.MaskHla(requests, typings);

            await macBuilder.Received().ConvertRandomLocusHlaToMacs(
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

            await locusHlaMasker.MaskHla(requests, typings);

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

            await locusHlaMasker.MaskHla(requests, typings);

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

            await locusHlaMasker.MaskHla(requests, typings);

            await hlaDeleter.DidNotReceive().DeleteRandomLocusHla(Arg.Any<TransformationRequest>());

            await xxCodeBuilder.Received(1).ConvertRandomLocusHlaToXxCodes(
                Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion && x.TotalSimulantCount == simulantCount));

            await macBuilder.Received(1).ConvertRandomLocusHlaToMacs(
                Arg.Is<TransformationRequest>(x =>
                x.ProportionToTransform == maskingProportion && x.TotalSimulantCount == simulantCount), 
                Arg.Any<string>());
        }
    }
}