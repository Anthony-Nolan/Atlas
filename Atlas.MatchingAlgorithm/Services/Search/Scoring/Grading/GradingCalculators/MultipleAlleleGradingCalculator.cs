using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
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
        private readonly IPermissiveMismatchCalculator permissiveMismatchCalculator;

        public MultipleAlleleGradingCalculator(IPermissiveMismatchCalculator permissiveMismatchCalculator)
        {
            this.permissiveMismatchCalculator = permissiveMismatchCalculator;
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
            var patientAlleles = patientLookupResult.GetInTermsOfSingleAlleleScoringMetadata();
            var donorAlleles = donorLookupResult.GetInTermsOfSingleAlleleScoringMetadata();

            var allGrades = patientAlleles.SelectMany(patientAllele => donorAlleles, GetSingleAlleleMatchGrade);

            return allGrades.Max();
        }

        private static IEnumerable<IHlaScoringLookupResult> GetSingleAlleleLookupResults(
            IHlaScoringLookupResult lookupResult)
        {
            var singleAlleleInfos = GetSingleAlleleInfos(lookupResult);

            return singleAlleleInfos.Select(alleleInfo => new HlaScoringLookupResult(
                lookupResult.Locus,
                alleleInfo.AlleleName,
                alleleInfo,
                TypingMethod.Molecular));
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
                permissiveMismatchCalculator,
                patientLookupResult.HlaScoringInfo,
                donorLookupResult.HlaScoringInfo);

            return calculator.CalculateGrade(patientLookupResult, donorLookupResult);
        }
    }
}