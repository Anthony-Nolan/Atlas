using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when both typings represent multiple alleles,
    /// or when one is multiple and the other is a single allele.
    /// </summary>
    public interface IMultipleAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class MultipleAlleleGradingCalculator :
        GradingCalculatorBase,
        IMultipleAlleleGradingCalculator
    {
        private IDpb1TceGroupsLookupService dpb1TceGroupsLookupService;

        public MultipleAlleleGradingCalculator(IDpb1TceGroupsLookupService dpb1TceGroupsLookupService)
        {
            this.dpb1TceGroupsLookupService = dpb1TceGroupsLookupService;
        }

        protected override bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            var bothAreMultiple =
                patientInfo is MultipleAlleleScoringInfo &&
                donorInfo is MultipleAlleleScoringInfo;

            var patientIsMultipleDonorIsSingle =
                patientInfo is MultipleAlleleScoringInfo &&
                donorInfo is SingleAlleleScoringInfo;

            var patientIsSingleDonorIsMultiple =
                patientInfo is SingleAlleleScoringInfo &&
                donorInfo is MultipleAlleleScoringInfo;

            return bothAreMultiple || patientIsMultipleDonorIsSingle || patientIsSingleDonorIsMultiple;
        }

        /// <summary>
        /// Returns the maximum grade possible after grading every combination of patient and donor allele.
        /// </summary>
        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            var patientAlleles = GetSingleAlleleLookupResults(patientLookupResult);
            var donorAlleles = GetSingleAlleleLookupResults(donorLookupResult);

            var allGrades = patientAlleles.SelectMany(patientAllele => donorAlleles, GetSingleAlleleMatchGrade);

            return allGrades.Max();
        }

        private static IEnumerable<IHlaScoringLookupResult> GetSingleAlleleLookupResults(
            IHlaScoringLookupResult lookupResult)
        {
            var singleAlleleInfos = GetSingleAlleleInfos(lookupResult);

            return singleAlleleInfos.Select(alleleInfo => new HlaScoringLookupResult(
                lookupResult.MatchLocus,
                alleleInfo.AlleleName,
                LookupNameCategory.OriginalAllele,
                alleleInfo));
        }

        private static IEnumerable<SingleAlleleScoringInfo> GetSingleAlleleInfos(
            IHlaScoringLookupResult lookupResult)
        {
            var scoringInfo = lookupResult.HlaScoringInfo;

            switch (scoringInfo)
            {
                case MultipleAlleleScoringInfo info:
                    return info.AlleleScoringInfos;
                case SingleAlleleScoringInfo info:
                    return new[] { info };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MatchGrade GetSingleAlleleMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            var calculator = GradingCalculatorFactory.GetGradingCalculator(
                dpb1TceGroupsLookupService,
                patientLookupResult.HlaScoringInfo,
                donorLookupResult.HlaScoringInfo);

            return calculator.CalculateGrade(patientLookupResult, donorLookupResult);
        }
    }
}