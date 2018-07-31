using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
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
        /// Returns the maximum grade possible from every possible combination of patient to donor allele.
        /// </summary>
        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            var patientAlleles = GetSingleAlleleLookupResults(patientLookupResult);
            var donorAlleles = GetSingleAlleleLookupResults(donorLookupResult);

            var singleAlleleCalculator = new SingleAlleleGradingCalculator();
            var allGrades = patientAlleles.SelectMany(patientAllele => donorAlleles,
                (patientAllele, donorAllele) => singleAlleleCalculator.CalculateGrade(patientAllele, donorAllele));

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
                case var info when scoringInfo is MultipleAlleleScoringInfo:
                    return ((MultipleAlleleScoringInfo) info).AlleleScoringInfos;
                case var info when scoringInfo is SingleAlleleScoringInfo:
                    return new[] { (SingleAlleleScoringInfo) info };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}