using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class VerificationResultsCompilerTests
    {
        private IVerificationRunRepository runRepository;
        private IVerificationResultsRepository resultsRepository;

        private IVerificationResultsCompiler resultsCompiler;

        [SetUp]
        public void SetUp()
        {
            runRepository = Substitute.For<IVerificationRunRepository>();
            resultsRepository = Substitute.For<IVerificationResultsRepository>();

            resultsCompiler = new VerificationResultsCompiler(runRepository, resultsRepository);

            runRepository.GetSearchLociCount(default).ReturnsForAnyArgs(5);
            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(new List<PdpPrediction>());
            resultsRepository.GetMatchedGenotypePdps(default).ReturnsForAnyArgs(new List<PatientDonorPair>());
        }

        [Test]
        public async Task CompileVerificationResults_ReturnsOneResultPerProbabilityBetween0To100Inclusive()
        {
            var results = await resultsCompiler.CompileVerificationResults(new VerificationResultsRequest());

            // 101 results: 0-100% inclusive
            results.Count().Should().Be(101);
        }

        [Test]
        public async Task CompileVerificationResults_GetsMaskedPdpPredictions()
        {
            var request = new VerificationResultsRequest
            {
                VerificationRunId = 123,
                MismatchCount = 2
            };

            await resultsCompiler.CompileVerificationResults(request);

            await resultsRepository.Received().GetMaskedPdpPredictions(Arg.Is<PdpPredictionsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.MismatchCount == request.MismatchCount));
        }

        [Test]
        public async Task CompileVerificationResults_GetsSearchLociCount()
        {
            var request = new VerificationResultsRequest
            {
                VerificationRunId = 123
            };

            await resultsCompiler.CompileVerificationResults(request);

            await runRepository.Received().GetSearchLociCount(request.VerificationRunId);
        }

        [Test]
        public async Task CompileVerificationResults_GetsMatchedGenotypePdpsUsingCorrectMatchCount()
        {
            const int searchLociCount = 3;
            const int mismatchCount = 1;
            const int expectedMatchCount = 5;

            runRepository.GetSearchLociCount(default).ReturnsForAnyArgs(searchLociCount);

            var request = new VerificationResultsRequest
            {
                VerificationRunId = 456,
                MismatchCount = mismatchCount
            };
            await resultsCompiler.CompileVerificationResults(request);

            await resultsRepository.Received().GetMatchedGenotypePdps(Arg.Is<MatchedPdpsRequest>(x =>
                x.VerificationRunId == request.VerificationRunId &&
                x.MatchCount == expectedMatchCount));
        }

        [Test] 
        public async Task CompileVerificationResults_CountsTotalPdps()
        {
            const int probabilityCount = 101;
            const int expectedCount = 2;
            
            var probabilities = Enumerable.Range(0, probabilityCount)
                .Select(x => (decimal)x/100)
                .Replicate(expectedCount);
            
            var predictions = PdpPredictionBuilder.Default
                .With(x => x.Probability, probabilities)
                .Build(probabilityCount * expectedCount)
                .ToList();

            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);

            var results = await resultsCompiler.CompileVerificationResults(new VerificationResultsRequest());

            results.Select(r => r.TotalPdpCount).Should().AllBeEquivalentTo(expectedCount);
        }

        [Test]
        public async Task CompileVerificationResults_CountsActuallyMatchedPdps()
        {
            const int probabilityCount = 101;
            const int expectedCount = 3;
            
            var probabilities = Enumerable.Range(0, probabilityCount)
                .Select(x => (decimal)x / 100)
                .Replicate(expectedCount);
            
            var predictions = PdpPredictionBuilder.Default
                .With(x => x.Probability, probabilities)
                .Build(probabilityCount * expectedCount)
                .ToList();
            
            // i.e., every prediction should be classified as actually matched
            var pdps = predictions.Select(p => (PatientDonorPair)p);

            resultsRepository.GetMaskedPdpPredictions(default).ReturnsForAnyArgs(predictions);
            resultsRepository.GetMatchedGenotypePdps(default).ReturnsForAnyArgs(pdps);

            var results = await resultsCompiler.CompileVerificationResults(new VerificationResultsRequest());

            results.Select(r => r.ActuallyMatchedPdpCount).Should().AllBeEquivalentTo(expectedCount);
        }

        [Test]
        public async Task CompileVerificationResults_AssignsDefaultResultForMissingProbability()
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

            var results = (await resultsCompiler.CompileVerificationResults(new VerificationResultsRequest())).ToList();
            var hundredPercentResult = results.Single(r => r.Probability == 100);

            results.Count.Should().Be(101);
            hundredPercentResult.ActuallyMatchedPdpCount.Should().Be(0);
            hundredPercentResult.TotalPdpCount.Should().Be(0);
        }
    }
}
