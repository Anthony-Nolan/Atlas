using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
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
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata)
        {
            var patientInfo = new AlleleGradingInfo(
                patientMetadata.Locus,
                (SingleAlleleScoringInfo)patientMetadata.HlaScoringInfo);

            var donorInfo = new AlleleGradingInfo(
                donorMetadata.Locus,
                (SingleAlleleScoringInfo)donorMetadata.HlaScoringInfo);

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
            return patientInfo.Allele.Name == donorInfo.Allele.Name;
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