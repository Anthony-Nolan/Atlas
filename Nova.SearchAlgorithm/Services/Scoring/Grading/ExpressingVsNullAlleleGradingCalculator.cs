using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// Calculates match grades when one allele is expressing and the other is null-expressing.
    /// </summary>
    public class ExpressingVsNullAlleleGradingCalculator : AlleleGradingCalculatorBase
    {
        public override MatchGrade GetMatchGrade(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return MatchGrade.Mismatch;
        }
    }
}