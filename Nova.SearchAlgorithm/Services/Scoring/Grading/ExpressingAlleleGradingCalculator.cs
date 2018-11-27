﻿using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// Calculates match grades when both patient and donor alleles are expressing.
    /// </summary>
    public class ExpressingAlleleGradingCalculator : AlleleGradingCalculatorBase
    {
        private IDpb1TceGroupsLookupService dpb1TceGroupsLookupService;

        public ExpressingAlleleGradingCalculator(IDpb1TceGroupsLookupService dpb1TceGroupsLookupService)
        {
            this.dpb1TceGroupsLookupService = dpb1TceGroupsLookupService;
        }

        protected override MatchGrade GetAlleleMatchGrade(
            AlleleGradingInfo patientInfo,
            AlleleGradingInfo donorInfo)
        {
            // Order of the following checks is critical to the grade outcome

            if (IsGDnaMatch(patientInfo, donorInfo))
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
        /// Do both alleles have same name & full gDNA sequences?
        /// </summary>
        private static bool IsGDnaMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return AreSameAllele(patientInfo, donorInfo) && 
                IsFullGDnaSequence(patientInfo.ScoringInfo.AlleleTypingStatus);
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

            if (AreSameAllele(patientInfo, donorInfo))
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
            return string.Equals(
                patientInfo.Allele.TwoFieldNameWithoutExpressionSuffix,
                donorInfo.Allele.TwoFieldNameWithoutExpressionSuffix) &&
                AreBothSequencesFullLength(patientInfo.ScoringInfo, donorInfo.ScoringInfo);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same G Group?
        /// </summary>
        private static bool IsGGroupMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return string.Equals(patientInfo.ScoringInfo.MatchingGGroup, donorInfo.ScoringInfo.MatchingGGroup);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same P Group?
        /// </summary>
        private static bool IsPGroupMatch(AlleleGradingInfo patientInfo, AlleleGradingInfo donorInfo)
        {
            return string.Equals(patientInfo.ScoringInfo.MatchingPGroup, donorInfo.ScoringInfo.MatchingPGroup);
        }

        protected static bool AreBothSequencesFullLength(
            SingleAlleleScoringInfo patientInfo,
            SingleAlleleScoringInfo donorInfo
        )
        {
            return
                patientInfo.AlleleTypingStatus.SequenceStatus == SequenceStatus.Full &&
                donorInfo.AlleleTypingStatus.SequenceStatus == SequenceStatus.Full;
        }
    }
}