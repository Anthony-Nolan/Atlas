using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when comparing two original allele typings.
    /// </summary>
    public interface IAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class AlleleGradingCalculator :
        GradingCalculatorBase,
        IAlleleGradingCalculator
    {
        private class AlleleGradingInfo
        {
            public SingleAlleleScoringInfo ScoringInfo { get; }
            public AlleleTyping Allele { get; }

            public AlleleGradingInfo(MatchLocus matchLocus, IHlaScoringInfo scoringInfo)
            {
                ScoringInfo = (SingleAlleleScoringInfo) scoringInfo;
                Allele = new AlleleTyping(matchLocus, ScoringInfo.AlleleName);
            }
        }

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

            // Order of the following checks is critical to the grade outcome

            if (IsExpressingVsNullMismatch(patientInfo, donorInfo))
            {
                return MatchGrade.Mismatch;
            }
            else if (IsGDnaMatch(patientInfo, donorInfo))
            {
                return MatchGrade.GDna;
            }
            else if (IsCDnaMatch(patientInfo, donorInfo))
            {
                return MatchGrade.CDna;
            }
            else if (IsProteinMatch(patientInfo, donorInfo))
            {
                return MatchGrade.Protein;
            }
            else if (IsGGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.GGroup;
            }
            else if (IsPGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.PGroup;
            }

            return MatchGrade.Mismatch;
        }

        /// <summary>
        /// Is one allele expressing and the other null expressing?
        /// </summary>
        private static bool IsExpressingVsNullMismatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return !patientInfo.Allele.IsNullExpresser == donorInfo.Allele.IsNullExpresser;
        }

        /// <summary>
        /// Do both alleles have same name & full gDNA sequences?
        /// </summary>
        private static bool IsGDnaMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return
                patientInfo.ScoringInfo.Equals(donorInfo.ScoringInfo) &&
                AreBothSequencesFullLength(patientInfo.ScoringInfo, donorInfo.ScoringInfo) &&
                donorInfo.ScoringInfo.AlleleTypingStatus.DnaCategory == DnaCategory.GDna;
        }

        /// <summary>
        /// Do both alleles have the same name AND have full cDNA; OR
        /// Do both alleles share the same first three fields AND have full sequences?
        /// </summary>
        private static bool IsCDnaMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            if (!AreBothSequencesFullLength(
                patientInfo.ScoringInfo,
                donorInfo.ScoringInfo))
            {
                return false;
            }

            if (patientInfo.ScoringInfo.Equals(donorInfo.ScoringInfo))
            {
                return true;
            }

            // Cannot get three field name for one or both alleles
            if (!patientInfo.Allele.TryGetThreeFieldName(out var patientAlleleName) ||
                !donorInfo.Allele.TryGetThreeFieldName(out var donorAlleleName))
            {
                return false;
            }

            return string.Equals(patientAlleleName, donorAlleleName);
        }

        /// <summary>
        /// Do both expressing alleles have the same first two fields AND have full sequences?
        /// </summary>
        private static bool IsProteinMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return
                AreBothAllelesExpressing(patientInfo.Allele, donorInfo.Allele) &&
                string.Equals(patientInfo.Allele.TwoFieldName, donorInfo.Allele.TwoFieldName) &&
                AreBothSequencesFullLength(patientInfo.ScoringInfo, donorInfo.ScoringInfo);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same G Group?
        /// </summary>
        private static bool IsGGroupMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return
                 AreBothAllelesExpressing(patientInfo.Allele, donorInfo.Allele) &&
                 string.Equals(patientInfo.ScoringInfo.MatchingGGroup, donorInfo.ScoringInfo.MatchingGGroup);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same P Group?
        /// </summary>
        private static bool IsPGroupMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return
                AreBothAllelesExpressing(patientInfo.Allele, donorInfo.Allele) &&
                string.Equals(patientInfo.ScoringInfo.MatchingPGroup, donorInfo.ScoringInfo.MatchingPGroup);
        }

        private static bool AreBothSequencesFullLength(
            SingleAlleleScoringInfo patientInfo,
            SingleAlleleScoringInfo donorInfo
        )
        {
            return
                patientInfo.AlleleTypingStatus.SequenceStatus == SequenceStatus.Full &&
                donorInfo.AlleleTypingStatus.SequenceStatus == SequenceStatus.Full;
        }

        private static bool AreBothAllelesExpressing(
            AlleleTyping patientAllele,
            AlleleTyping donorAllele)
        {
            return !patientAllele.IsNullExpresser && !donorAllele.IsNullExpresser;
        }
    }
}