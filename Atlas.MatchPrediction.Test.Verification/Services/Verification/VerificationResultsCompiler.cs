using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    internal interface IVerificationResultsCompiler
    {
        Task<IEnumerable<VerificationResult>> CompileVerificationResults(CompileResultsRequest request);
    }

    internal class CompileResultsRequest
    {
        public int VerificationRunId { get; set; }
        public int MismatchCount { get; set; }

        /// <summary>
        /// Leave null for Cross-Loci prediction.
        /// </summary>
        public Locus? Locus { get; set; }

        public string PredictionName => Locus == null ? "CrossLoci" : Locus.ToString();

        public override string ToString()
        {
            return $"RunId: {VerificationRunId}, Mismatch-count: {MismatchCount}, Prediction: {PredictionName}";
        }
    }

    internal class VerificationResultsCompiler : IVerificationResultsCompiler
    {
        private readonly IVerificationRunRepository runRepository;
        private readonly IVerificationResultsRepository resultsRepository;

        public VerificationResultsCompiler(
            IVerificationRunRepository runRepository,
            IVerificationResultsRepository resultsRepository)
        {
            this.runRepository = runRepository;
            this.resultsRepository = resultsRepository;
        }

        public async Task<IEnumerable<VerificationResult>> CompileVerificationResults(CompileResultsRequest request)
        {
            var defaultResults = BuildDefaultResults();
            var verificationResults = await GetVerificationResults(request);

            // ensure that every probability value between 0-100% has a result
            return
                from defaultResult in defaultResults
                join verificationResult in verificationResults
                    on defaultResult.Probability equals verificationResult.Probability into gj
                from result in gj.DefaultIfEmpty()
                select result ?? defaultResult;
        }

        private static IEnumerable<VerificationResult> BuildDefaultResults()
        {
            const int minProbabilityValue = 0;
            return Enumerable.Range(minProbabilityValue, 101)
                .Select(probability => new VerificationResult
                {
                    Probability = probability,
                    ActuallyMatchedPdpCount = 0,
                    TotalPdpCount = 0
                });
        }

        private async Task<IReadOnlyCollection<VerificationResult>> GetVerificationResults(CompileResultsRequest request)
        {
            var actualMatchStatuses = await GetActualMatchStatusOfPdps(request);

            return actualMatchStatuses.GroupBy(status => status.Prediction.ProbabilityAsRoundedPercentage)
                .Select(grp => new VerificationResult
                {
                    Probability = grp.Key,
                    ActuallyMatchedPdpCount = grp.Count(status => status.WasActuallyMatched),
                    TotalPdpCount = grp.Count()
                }).ToList();
        }

        private async Task<IEnumerable<ActualMatchStatus>> GetActualMatchStatusOfPdps(CompileResultsRequest request)
        {
            var predictions = (await GetMaskedPdpPredictions(request)).ToList();
            var genotypePdps = await GetMatchedGenotypePdps(request);

            return
                from prediction in predictions
                join genotypePdp in genotypePdps on
                    new { prediction.PatientGenotypeSimulantId, prediction.DonorGenotypeSimulantId } equals
                    new { genotypePdp.PatientGenotypeSimulantId, genotypePdp.DonorGenotypeSimulantId } into gj
                from pdp in gj.DefaultIfEmpty()
                select new ActualMatchStatus
                {
                    Prediction = prediction,
                    WasActuallyMatched = pdp != null
                };
        }

        private async Task<IEnumerable<PdpPrediction>> GetMaskedPdpPredictions(CompileResultsRequest request)
        {
            return await resultsRepository.GetMaskedPdpPredictions(new PdpPredictionsRequest
            {
                VerificationRunId = request.VerificationRunId,
                MismatchCount = request.MismatchCount,
                Locus = request.Locus
            });
        }

        private async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdps(CompileResultsRequest request)
        {
            return request.Locus == null
                ? await GetMatchedGenotypePdpsForCrossLociPrediction(request)
                : await GetMatchedGenotypePdpsForSingleLocusPrediction(request);
        }

        private async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForCrossLociPrediction(CompileResultsRequest request)
        {
            return await resultsRepository.GetMatchedGenotypePdpsForCrossLociPrediction(new MatchedPdpsRequest
            {
                VerificationRunId = request.VerificationRunId,
                MatchCount = CalculateMatchCount(
                    await runRepository.GetSearchLociCount(request.VerificationRunId), request.MismatchCount)
            });
        }

        private async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdpsForSingleLocusPrediction(CompileResultsRequest request)
        {
            if (request.Locus == null)
            {
                throw new ArgumentNullException(nameof(request.Locus));
            }

            return await resultsRepository.GetMatchedGenotypePdpsForSingleLocusPrediction(new SingleLocusMatchedPdpsRequest
            {
                VerificationRunId = request.VerificationRunId,
                MatchCount = CalculateMatchCount(1, request.MismatchCount),
                Locus = request.Locus.Value
            });
        }

        private int CalculateMatchCount(int lociCount, int mismatchCount)
        {
            var positionCount = 2 * lociCount;

            if (mismatchCount > positionCount)
            {
                throw new ArgumentException(
                    $"Mismatch count ({mismatchCount}) greater than total position count ({positionCount}).",
                    nameof(mismatchCount));
            }

            return positionCount - mismatchCount;
        }

        private class ActualMatchStatus
        {
            public PdpPrediction Prediction { get; set; }
            public bool WasActuallyMatched { get; set; }
        }
    }
}
