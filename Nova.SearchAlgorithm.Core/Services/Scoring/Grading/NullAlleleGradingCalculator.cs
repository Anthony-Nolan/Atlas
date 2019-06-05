using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// Calculates match grades when both patient and donor alleles are null-expressing.
    /// </summary>
    public class NullAlleleGradingCalculator : AlleleGradingCalculatorBase
    {
        protected override MatchGrade GetAlleleMatchGrade(
            AlleleGradingInfo patientInfo, 
            AlleleGradingInfo donorInfo)
        {
            return AreSameAllele(patientInfo, donorInfo) 
                ? GetNullAlleleMatchGrade(patientInfo.ScoringInfo.AlleleTypingStatus) 
                : MatchGrade.NullMismatch;
        }

        private static MatchGrade GetNullAlleleMatchGrade(AlleleTypingStatus typingStatus)
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