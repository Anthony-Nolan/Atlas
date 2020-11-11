using System.Collections.Generic;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class VerificationResult
    {
        public CompileResultsRequest Request { get; set; }
        public IEnumerable<ActualVersusExpectedResult> ActualVersusExpectedResults { get; set; }
        public int? TotalPdpCount { get; set; }
        public decimal? WeightedCityBlockDistance { get; set; }
        public LinearRegression WeightedLinearRegression { get; set; }
    }

    internal class ActualVersusExpectedResult
    {
        public int Probability { get; set; }
        public int ActuallyMatchedPdpCount { get; set; }
        public int TotalPdpCount { get; set; }
    }

    internal class LinearRegression
    {
        public decimal Slope { get; set; }
        public decimal Intercept { get; set; }
    }
}
