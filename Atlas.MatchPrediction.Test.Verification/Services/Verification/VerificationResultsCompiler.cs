using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    internal interface IVerificationResultsCompiler
    {
        Task<IEnumerable<VerificationResult>> CompileVerificationResults(VerificationResultsRequest request);
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

        public async Task<IEnumerable<VerificationResult>> CompileVerificationResults(VerificationResultsRequest request)
        {
            var defaultResults = BuildDefaultVerificationResults();
            var actualResults = await GetActualVerificationResults(request);

            // ensure that every probability value between 0-100% has a result
            return
                from defaultResult in defaultResults
                join actualResult in actualResults
                    on defaultResult.Probability equals actualResult.Probability into gj
                from result in gj.DefaultIfEmpty()
                select result ?? defaultResult;
        }

        private static IEnumerable<VerificationResult> BuildDefaultVerificationResults()
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

        private async Task<IReadOnlyCollection<VerificationResult>> GetActualVerificationResults(VerificationResultsRequest request)
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

        private async Task<IEnumerable<ActualMatchStatus>> GetActualMatchStatusOfPdps(VerificationResultsRequest request)
        {
            var predictions = await GetMaskedPdpPredictions(request);
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

        private async Task<IEnumerable<PdpPrediction>> GetMaskedPdpPredictions(VerificationResultsRequest request)
        {
            return await resultsRepository.GetMaskedPdpPredictions(new PdpPredictionsRequest
            {
                VerificationRunId = request.VerificationRunId,
                MismatchCount = request.MismatchCount
            });
        }

        private async Task<IEnumerable<PatientDonorPair>> GetMatchedGenotypePdps(VerificationResultsRequest request)
        {
            var searchLociCount = await runRepository.GetSearchLociCount(request.VerificationRunId);

            return await resultsRepository.GetMatchedGenotypePdps(new MatchedPdpsRequest
            {
                VerificationRunId = request.VerificationRunId,
                MatchCount = searchLociCount * 2 - request.MismatchCount
            });
        }

        private class ActualMatchStatus
        {
            public PdpPrediction Prediction { get; set; }
            public bool WasActuallyMatched { get; set; }
        }
    }
}
