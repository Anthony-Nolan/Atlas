using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public abstract class AlleleGradingCalculatorBase : GradingCalculatorBase
    {
        #region Override GradingCalculatorBase Methods

        protected override bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return patientInfo is SingleAlleleScoringInfo &&
                   donorInfo is SingleAlleleScoringInfo;
        }

        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            var patientInfo = new AlleleGradingInfo(
                patientLookupResult.MatchLocus,
                patientLookupResult.HlaScoringInfo);

            var donorInfo = new AlleleGradingInfo(
                donorLookupResult.MatchLocus,
                donorLookupResult.HlaScoringInfo);

            return GetAlleleMatchGrade(patientInfo, donorInfo);
        }

        #endregion

        protected abstract MatchGrade GetAlleleMatchGrade(
            AlleleGradingInfo patientInfo, 
            AlleleGradingInfo donorInfo);

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