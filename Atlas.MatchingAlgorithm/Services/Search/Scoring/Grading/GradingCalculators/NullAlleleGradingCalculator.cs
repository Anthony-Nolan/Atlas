using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
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