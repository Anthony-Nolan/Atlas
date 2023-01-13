using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class ActualVersusExpectedResultsCompilerTests
    {
        private IVerificationRunRepository runRepository;
        private IVerificationResultsRepository resultsRepository;
        private IGenotypeSimulantsInfoCache infoCache;

        private IActualVersusExpectedResultsCompiler resultsCompiler;

        [SetUp]
        public void SetUp()
        {
            runRepository = Substitute.For<IVerificationRunRepository>();
            resultsRepository = Substitute.For<IVerificationResultsRepository>();
            infoCache = Substitute.For<IGenotypeSimulantsInfoCache>();

            resultsCompiler = new ActualVersusExpectedResultsCompiler(runRepository, resultsRepository, infoCache);

            runRepository.GetSearchLociCount(default).ReturnsForAnyArgs(5);
            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(new List<PdpPrediction>());
            resultsRepository.GetMatchedGenotypePdpsForCrossLociPrediction(default).ReturnsForAnyArgs(new List<PatientDonorPair>());
            resultsRepository.GetMatchedGenotypePdpsForSingleLocusPrediction(default).ReturnsForAnyArgs(new List<PatientDonorPair>());
        }

        [Test]
        public async Task CompileResults_ReturnsOneAvEResultPerProbabilityBetween0To100Inclusive()
        {
            var results = await resultsCompiler.CompileResults(new CompileResultsRequest());

            // 101 results: 0-100% inclusive
            results.Count().Should().Be(101);
        }

        [Test]
        public async Task CompileResults_GetsMaskedPdpPredictions()
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 123,
                MismatchCount = 2
            };

            await resultsCompiler.CompileResults(request);

            await resultsRepository.Received().GetMaskedPdpPredictions(Arg.Is<PdpPredictionsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.MismatchCount == request.MismatchCount));
        }

        [Test]
        public async Task CompileResults_WhenCrossLociPrediction_GetsSearchLociCount()
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 123,
                Locus = null
            };

            await resultsCompiler.CompileResults(request);

            await runRepository.Received().GetSearchLociCount(request.VerificationRunId);
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_DoesNotGetSearchLociCount()
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 123,
                Locus = Locus.A
            };

            await resultsCompiler.CompileResults(request);

            await runRepository.DidNotReceive().GetSearchLociCount(Arg.Any<int>());
        }

        [Test]
        public async Task CompileResults_WhenCrossLociPrediction_GetsMatchedGenotypePdpsUsingCorrectMatchCount()
        {
            const int searchLociCount = 3;
            const int mismatchCount = 1;
            const int expectedMatchCount = 5;

            runRepository.GetSearchLociCount(default).ReturnsForAnyArgs(searchLociCount);

            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                MismatchCount = mismatchCount
            };
            await resultsCompiler.CompileResults(request);

            await resultsRepository.Received().GetMatchedGenotypePdpsForCrossLociPrediction(Arg.Is<MatchedPdpsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.MatchCount == expectedMatchCount));
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountLessThan2_DoesNotGetAllPossiblePatientDonorPairs(
            [Range(0, 1)] int mismatchCount)
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                Locus = Locus.A,
                MismatchCount = mismatchCount
            };
            await resultsCompiler.CompileResults(request);

            await infoCache.DidNotReceive().GetOrAddAllPossibleGenotypePatientDonorPairs(Arg.Any<int>());
        }

        [TestCase(0, 2)]
        [TestCase(1, 1)]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountLessThan2_GetsMatchedGenotypePdpsUsingCorrectMatchCount(
            int mismatchCount,
            int expectedMatchCount)
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                Locus = Locus.A,
                MismatchCount = mismatchCount
            };
            await resultsCompiler.CompileResults(request);

            await resultsRepository.Received(1).GetMatchedGenotypePdpsForSingleLocusPrediction(Arg.Is<SingleLocusMatchedPdpsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.Locus == request.Locus &&
                x.MatchCount == expectedMatchCount));
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountIs2_GetsAllPossiblePatientDonorPairs()
        {
            const int runId = 456;

            var request = new CompileResultsRequest
            {
                VerificationRunId = runId,
                Locus = Locus.A,
                MismatchCount = 2
            };
            await resultsCompiler.CompileResults(request);

            await infoCache.Received().GetOrAddAllPossibleGenotypePatientDonorPairs(runId);
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountIs2_GetsGenotypePdpsWithOneOrTwoMatches()
        {
            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                Locus = Locus.A,
                MismatchCount = 2
            };
            await resultsCompiler.CompileResults(request);

            await resultsRepository.Received(1).GetMatchedGenotypePdpsForSingleLocusPrediction(Arg.Is<SingleLocusMatchedPdpsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.Locus == request.Locus &&
                x.MatchCount == 1));

            await resultsRepository.Received(1).GetMatchedGenotypePdpsForSingleLocusPrediction(Arg.Is<SingleLocusMatchedPdpsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.Locus == request.Locus &&
                x.MatchCount == 2));
        }

        [Test]
        public async Task CompileResults_CountsTotalPdpsPerProbability()
        {
            const int probabilityCount = 101;
            const int expectedCount = 2;

            var probabilities = Enumerable.Range(0, probabilityCount)
                .Select(x => (decimal)x / 100)
                .Replicate(expectedCount);

            var predictions = PdpPredictionBuilder.Default
                .With(x => x.Probability, probabilities)
                .Build(probabilityCount * expectedCount)
                .ToList();

            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            var results = await resultsCompiler.CompileResults(new CompileResultsRequest());

            results.Select(r => r.TotalPdpCount).Should().AllBeEquivalentTo(expectedCount);
        }

        [Test]
        public async Task CompileResults_WhenCrossLociPrediction_CountsActuallyMatchedPdpsPerProbability()
        {
            const int probabilityCount = 101;
            const int pdpCount = 3;

            var predictions = BuildPdpPredictions(probabilityCount, pdpCount);
            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            // i.e., every prediction should be classified as actually matched
            var pdps = predictions.Select(p => (PatientDonorPair)p);
            resultsRepository.GetMatchedGenotypePdpsForCrossLociPrediction(default).ReturnsForAnyArgs(pdps);

            var results = await resultsCompiler.CompileResults(new CompileResultsRequest());

            results.Select(r => r.ActuallyMatchedPdpCount).Should().AllBeEquivalentTo(pdpCount);
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountLessThan2_CountsActuallyMatchedPdpsPerProbability(
            [Range(0, 1)] int mismatchCount)
        {
            const int probabilityCount = 101;
            const int pdpCount = 4;

            var predictions = BuildPdpPredictions(probabilityCount, pdpCount);
            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            // i.e., every prediction should be classified as actually matched
            var pdps = predictions.Select(p => (PatientDonorPair)p);
            resultsRepository.GetMatchedGenotypePdpsForSingleLocusPrediction(default).ReturnsForAnyArgs(pdps);

            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                Locus = Locus.A,
                MismatchCount = mismatchCount
            };
            var results = await resultsCompiler.CompileResults(request);

            results.Select(r => r.ActuallyMatchedPdpCount).Should().AllBeEquivalentTo(pdpCount);
        }

        [Test]
        public async Task CompileResults_WhenSingleLocusPrediction_AndMismatchCountIs2_CountsActuallyMatchedPdpsPerProbability()
        {
            const int probabilityCount = 101;
            const int totalPdpCount = 4;
            const int oneMatchPdpCount = 2;
            const int twoMatchPdpCount = 1;
            const int expectedZeroMatchPdpCount = totalPdpCount - oneMatchPdpCount - twoMatchPdpCount;

            var predictions = BuildPdpPredictions(probabilityCount, totalPdpCount);
            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            // Locus PDPs can only have a match count of 0, 1 or 2.
            // Arrange so that each P(0/2) probability value has the correct number of 1/2 and 2/2 matched pdps
            // so that the counting of 0/2 matched pdps can be tested.
            var allPdps = predictions.Select(p => (PatientDonorPair)p).ToList();
            infoCache.GetOrAddAllPossibleGenotypePatientDonorPairs(default).ReturnsForAnyArgs(allPdps);

            var pdpsWithOneMatch = predictions
                .Take(probabilityCount * oneMatchPdpCount)
                .Select(p => (PatientDonorPair)p);
            resultsRepository.GetMatchedGenotypePdpsForSingleLocusPrediction(
                Arg.Is<SingleLocusMatchedPdpsRequest>(x => x.MatchCount == 1)).Returns(pdpsWithOneMatch);

            var pdpsWithTwoMatches = predictions
                .Skip(probabilityCount * oneMatchPdpCount)
                .Take(probabilityCount * twoMatchPdpCount)
                .Select(p => (PatientDonorPair)p);
            resultsRepository.GetMatchedGenotypePdpsForSingleLocusPrediction(
                Arg.Is<SingleLocusMatchedPdpsRequest>(x => x.MatchCount == 2)).Returns(pdpsWithTwoMatches);

            var request = new CompileResultsRequest
            {
                VerificationRunId = 456,
                Locus = Locus.A,
                MismatchCount = 2
            };
            var results = await resultsCompiler.CompileResults(request);

            results.Select(r => r.ActuallyMatchedPdpCount).Should().AllBeEquivalentTo(expectedZeroMatchPdpCount);
        }

        [Test]
        public async Task CompileResults_AssignsDefaultResultForMissingProbability()
        {
            // will generate results for 0-99% inclusive; 100% will be missing.
            const int probabilityCount = 100;

            var probabilities = Enumerable.Range(0, probabilityCount)
                .Select(x => (decimal)x / 100);

            var predictions = PdpPredictionBuilder.Default
                .With(x => x.Probability, probabilities)
                .Build(probabilityCount)
                .ToList();

            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            var results = (await resultsCompiler.CompileResults(new CompileResultsRequest())).ToList();
            var hundredPercentResult = results.Single(r => r.Probability == 100);

            results.Count.Should().Be(101);
            hundredPercentResult.ActuallyMatchedPdpCount.Should().Be(0);
            hundredPercentResult.TotalPdpCount.Should().Be(0);
        }

        private static IReadOnlyCollection<PdpPrediction> BuildPdpPredictions(int probabilityCount, int totalPdpCount)
        {
            var probabilities = Enumerable.Range(0, probabilityCount)
                .Select(x => (decimal)x / 100)
                .Replicate(totalPdpCount);

            return PdpPredictionBuilder.Default
                .With(x => x.Probability, probabilities)
                .Build(probabilityCount * totalPdpCount)
                .ToList();
        }
    }
}