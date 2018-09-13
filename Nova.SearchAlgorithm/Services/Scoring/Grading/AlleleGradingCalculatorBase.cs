using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public abstract class AlleleGradingCalculatorBase
    {
        public abstract MatchGrade GetMatchGrade(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo);

        protected static bool AreSameAllele(
            AlleleGradingInfo patientInfo,
            AlleleGradingInfo donorInfo)
        {
            return patientInfo.ScoringInfo.Equals(donorInfo.ScoringInfo);
        }

        protected static bool IsFullGDnaSequence(AlleleTypingStatus typingStatus)
        {
            return IsFullSequence(typingStatus, DnaCategory.GDna);
        }

        protected static bool IsFullCDnaSequence(AlleleTypingStatus typingStatus)
        {
            return IsFullSequence(typingStatus, DnaCategory.CDna);
        }

        private static bool IsFullSequence(AlleleTypingStatus typingStatus, DnaCategory dnaCategory)
        {
            return typingStatus.SequenceStatus == SequenceStatus.Full &&
                   typingStatus.DnaCategory == dnaCategory;
        }
    }
}