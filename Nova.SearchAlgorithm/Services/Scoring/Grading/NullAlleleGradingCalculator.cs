using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public class NullAlleleGradingCalculator : AlleleGradingCalculatorBase
    {
        public override MatchGrade GetMatchGrade(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return AreSameAllele(patientInfo, donorInfo) 
                ? GetSameAlleleGrade(patientInfo.ScoringInfo.AlleleTypingStatus) 
                : MatchGrade.NullMismatch;
        }

        private static MatchGrade GetSameAlleleGrade(AlleleTypingStatus typingStatus)
        {
            if (IsFullGDnaSequence(typingStatus))
            {
                return MatchGrade.NullGDna;
            }
            else if (IsFullCDnaSequence(typingStatus))
            {
                return MatchGrade.NullCDna;
            }

            return MatchGrade.NullPartial;
        }
    }
}