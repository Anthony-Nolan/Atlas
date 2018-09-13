using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public class ExpressingVsNullAlleleGradingCalculator : AlleleGradingCalculatorBase
    {
        public override MatchGrade GetMatchGrade(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return MatchGrade.Mismatch;
        }
    }
}