using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when comparing two original allele typings.
    /// </summary>
    public interface IAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class AlleleGradingCalculator : IAlleleGradingCalculator
    {
        private class MatchGraderArgs
        {
            public AlleleTyping PatientAllele { get; }
            public AlleleTyping DonorAllele { get; }
            public SingleAlleleScoringInfo PatientScoringInfo { get; }
            public SingleAlleleScoringInfo DonorScoringInfo { get; }

            public MatchGraderArgs(
                MatchLocus matchLocus,
                SingleAlleleScoringInfo patientScoringInfo, 
                SingleAlleleScoringInfo donorScoringInfo)
            {
                PatientScoringInfo = patientScoringInfo;
                DonorScoringInfo = donorScoringInfo;
                PatientAllele = new AlleleTyping(matchLocus, patientScoringInfo.AlleleName);
                DonorAllele = new AlleleTyping(matchLocus, donorScoringInfo.AlleleName);
            }
        }

        public MatchGrade CalculateGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            if (patientLookupResult == null || donorLookupResult == null)
            {
                throw new ArgumentException("Cannot calculate grade for null lookup result(s).");
            }

            if (patientLookupResult.MatchLocus != donorLookupResult.MatchLocus)
            {
                throw new ArgumentException("Cannot calculate grade for different loci");
            }

            var patientInfo = patientLookupResult.HlaScoringInfo;
            var donorInfo = donorLookupResult.HlaScoringInfo;

            return (patientInfo is SingleAlleleScoringInfo pInfo && donorInfo is SingleAlleleScoringInfo dInfo)
                ? GetMatchGrade(new MatchGraderArgs(patientLookupResult.MatchLocus, pInfo, dInfo))
                : throw new ArgumentException($"Both lookup results must be of type: {typeof(SingleAlleleScoringInfo)}.");
        }

        private static MatchGrade GetMatchGrade(MatchGraderArgs args)
        {
            // Order of the following checks is critical to the grade outcome

            if (IsExpressingVsNullMismatch(args))
            {
                return MatchGrade.Mismatch;
            }

            if (IsGDnaMatch(args))
            {
                return MatchGrade.GDna;
            }

            if (IsCDnaMatch(args))
            {
                return MatchGrade.CDna;
            }

            if (IsProteinMatch(args))
            {
                return MatchGrade.Protein;
            }

            if (IsGGroupMatch(args))
            {
                return MatchGrade.GGroup;
            }

            if (IsPGroupMatch(args))
            {
                return MatchGrade.PGroup;
            }

            return MatchGrade.Mismatch;
        }

        /// <summary>
        /// Is one allele is expressing and the other null expressing?
        /// </summary>
        private static bool IsExpressingVsNullMismatch(MatchGraderArgs args)
        {
            return !args.PatientAllele.IsNullExpresser
                .Equals(args.DonorAllele.IsNullExpresser);
        }

        /// <summary>
        /// Do both alleles have same name & full gDNA sequences?
        /// </summary>
        private static bool IsGDnaMatch(MatchGraderArgs args)
        {
            return
                args.PatientScoringInfo.Equals(args.DonorScoringInfo) &&
                AreBothSequencesFullLength(args.PatientScoringInfo, args.DonorScoringInfo) &&
                args.DonorScoringInfo.AlleleTypingStatus.DnaCategory == DnaCategory.GDna;
        }

        /// <summary>
        /// Do both alleles have the same name AND have full cDNA; OR
        /// Do both alleles share the same first three fields AND have full sequences?
        /// </summary>
        private static bool IsCDnaMatch(MatchGraderArgs args)
        {
            if (!AreBothSequencesFullLength(
                args.PatientScoringInfo, 
                args.DonorScoringInfo))
            {
                return false;
            }

            if (args.PatientScoringInfo.Equals(args.DonorScoringInfo))
            {
                return true;
            }

            // Cannot get three field name for one or both alleles
            if (!args.PatientAllele.TryGetThreeFieldName(out var patientAlleleName) ||
                !args.DonorAllele.TryGetThreeFieldName(out var donorAlleleName))
            {
                return false;
            }

            return string.Equals(patientAlleleName, donorAlleleName);
        }

        /// <summary>
        /// Do both expressing alleles have the same first two fields AND have full c/gDNA?
        /// </summary>
        private static bool IsProteinMatch(MatchGraderArgs args)
        {
            return
                AreBothAllelesExpressing(args.PatientAllele, args.DonorAllele) &&
                string.Equals(args.PatientAllele.TwoFieldName, args.DonorAllele.TwoFieldName) &&
                AreBothSequencesFullLength(args.PatientScoringInfo, args.DonorScoringInfo);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same G Group?
        /// </summary>
        private static bool IsGGroupMatch(MatchGraderArgs args)
        {
           return
                !args.PatientAllele.IsNullExpresser && !args.DonorAllele.IsNullExpresser &&
                string.Equals(args.PatientScoringInfo.MatchingGGroup, args.DonorScoringInfo.MatchingGGroup);
        }

        /// <summary>
        /// Do both expressing alleles belong to the same P Group?
        /// </summary>
        private static bool IsPGroupMatch(MatchGraderArgs args)
        {
            return
                !args.PatientAllele.IsNullExpresser && !args.DonorAllele.IsNullExpresser &&
                string.Equals(args.PatientScoringInfo.MatchingPGroup, args.DonorScoringInfo.MatchingPGroup);
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