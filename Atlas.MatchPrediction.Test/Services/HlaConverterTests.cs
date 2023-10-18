using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Services.HlaConversion;
using FluentAssertions;
using LochNessBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services
{
    /// <summary>
    /// Covers base logic of <see cref="HlaConverterBase"/> via one concrete implementation, <see cref="HlaToTargetCategoryConverter"/>.
    /// </summary>
    [TestFixture]
    internal class HlaConverterTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string HlaName = "hla";

        private const string HfSetHlaVersion = "hf-set-hla-version";
        private const string MatchingAlgorithmHlaVersion = "matching-hla-version";

        private static readonly IReadOnlyCollection<string> HfSetHmdResult = new []{"hf-set-hmd-result"};
        private static readonly IReadOnlyCollection<string> MatchingHmdResult = new[] { "matching-hmd-result" };

        private IHlaMetadataDictionary hfSetHmd;
        private IHlaMetadataDictionary matchingHmd;
        private Builder<HlaConverterInput> inputBuilder;

        private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
        private IHlaConverter converter;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
            converter = new HlaToTargetCategoryConverter(logger);

            hfSetHmd = Substitute.For<IHlaMetadataDictionary>();
            hfSetHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);
            hfSetHmd.ConvertHla(default, default, default).ReturnsForAnyArgs(HfSetHmdResult);

            matchingHmd = Substitute.For<IHlaMetadataDictionary>();
            matchingHmd.HlaNomenclatureVersion.Returns(MatchingAlgorithmHlaVersion);
            matchingHmd.ConvertHla(default, default, default).ReturnsForAnyArgs(MatchingHmdResult);

            inputBuilder = Builder<HlaConverterInput>.New;
        }

        #region DoesNotTryLookupUsingMatchingAlgorithmHmd tests

        [Test]
        public void HlaConverterInput_NoMatchingAlgorithmHmd_DoesNotRetryLookupUsingMatchingAlgorithmHmd()
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .Build();

            input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeTrue();
        }

        [Test]
        public void HlaConverterInput_HasMatchingAlgorithmHmdOfSameHlaVersion_DoesNotRetryLookupUsingMatchingAlgorithmHmd()
        {
            matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .Build();

            input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeTrue();
        }

        [Test]
        public void HlaConverterInput_HasMatchingAlgorithmHmdOfDifferentHlaVersion_DoesRetryLookupUsingMatchingAlgorithmHmd()
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .Build();

            input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeFalse();
        }

        #endregion

        #region ConvertHlaWithLoggingAndRetryOnFailure tests

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_ConvertsHla([Values] TargetHlaCategory target)
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            await hfSetHmd.Received().ConvertHla(DefaultLocus, HlaName, target);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_ReturnsResults(
            [Values] TargetHlaCategory target)
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            results.Should().BeEquivalentTo(HfSetHmdResult);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_DoesNotLogFailure(
            [Values] TargetHlaCategory target)
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            logger.DidNotReceiveWithAnyArgs().SendEvent(default);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_AndRetryEnabled_DoesNotRetryLookup(
            [Values] TargetHlaCategory target)
        {
            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            await matchingHmd.DidNotReceiveWithAnyArgs().ConvertHla(default, default, default);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_AndRetryDisabled_DoesNotRetryLookup(
            [Values] TargetHlaCategory target)
        {
            // disables retry
            matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            await matchingHmd.DidNotReceiveWithAnyArgs().ConvertHla(default, default, default);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_LogsFailure(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            logger.Received(1).SendEvent(Arg.Any<HlaConversionFailureEventModel>());
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryEnabled_RetriesLookupUsingMatchingHmd(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            await matchingHmd.Received().ConvertHla(DefaultLocus, HlaName, target);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryEnabled_ReturnsResults(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            results.Should().BeEquivalentTo(MatchingHmdResult);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryDisabled_DoesNotRetryLookup(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            // disables retry
            matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            await matchingHmd.DidNotReceiveWithAnyArgs().ConvertHla(default, default, default);
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryDisabled_ReturnsEmptyCollection(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            // disables retry
            matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_SecondLookupFails_LogsFirstAndSecondFailure(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            // arrange second lookup failure
            matchingHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            logger.Received(2).SendEvent(Arg.Any<HlaConversionFailureEventModel>());
        }

        [Test]
        public async Task ConvertHlaWithLoggingAndRetryOnFailure_SecondLookupFails_ReturnsEmptyCollection(
            [Values] TargetHlaCategory target)
        {
            // arrange first lookup failure
            hfSetHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            // arrange second lookup failure
            matchingHmd
                .ConvertHla(default, default, default)
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException(default, default, default));

            var input = inputBuilder
                .With(x => x.HfSetHmd, hfSetHmd)
                .With(x => x.MatchingAlgorithmHmd, matchingHmd)
                .With(x => x.TargetHlaCategory, target)
                .Build();

            var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

            results.Should().BeEmpty();
        }

        #endregion
    }
}
