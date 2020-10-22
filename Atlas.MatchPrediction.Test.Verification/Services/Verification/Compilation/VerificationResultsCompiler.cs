using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation
{
    internal interface IVerificationResultsCompiler
    {
        Task<VerificationResult> CompileVerificationResults(CompileResultsRequest request);
    }

    internal class VerificationResultsCompiler : IVerificationResultsCompiler
    {
        private readonly IActualVersusExpectedResultsCompiler avEResultsCompiler;

        public VerificationResultsCompiler(IActualVersusExpectedResultsCompiler avEResultsCompiler)
        {
            this.avEResultsCompiler = avEResultsCompiler;
        }

        public async Task<VerificationResult> CompileVerificationResults(CompileResultsRequest request)
        {
            var actualVersusExpectedResults = await avEResultsCompiler.CompileResults(request);

            return new VerificationResult
            {
                ActualVersusExpectedResults = actualVersusExpectedResults
            };
        }
    }
}
