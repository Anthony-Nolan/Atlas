using System;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    public static class GradingCalculatorFactory
    {
        public static IGradingCalculator GetGradingCalculator(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo
        )
        {
            // order of checks is critical to which calculator is returned

            if (donorInfo is NewAlleleScoringInfo)
            {
                return new NewAlleleGradingCalculator();
            }
            if (patientInfo is SerologyScoringInfo || donorInfo is SerologyScoringInfo)
            {
                return new SerologyGradingCalculator();
            }
            // If comparing a single null allele to a non-single allele,
            // as we are ignoring null alleles within allele strings, we can consider this a null vs. expressing comparison
            if (IsSingleNullAllele(patientInfo) ^ IsSingleNullAllele(donorInfo))
            {
                return new ExpressingVsNullAlleleGradingCalculator();
            }
            if (patientInfo is ConsolidatedMolecularScoringInfo || donorInfo is ConsolidatedMolecularScoringInfo)
            {
                return new ConsolidatedMolecularGradingCalculator();
            }
            if (patientInfo is MultipleAlleleScoringInfo || donorInfo is MultipleAlleleScoringInfo)
            {
                return new MultipleAlleleGradingCalculator();
            }
            if (patientInfo is SingleAlleleScoringInfo pInfo && donorInfo is SingleAlleleScoringInfo dInfo)
            {
                return GetSingleAlleleGradingCalculator(pInfo, dInfo);
            }

            throw new ArgumentException("No calculator available for provided patient and donor scoring infos.");
        }

        private static IGradingCalculator GetSingleAlleleGradingCalculator(
            SingleAlleleScoringInfo patientInfo,
            SingleAlleleScoringInfo donorInfo)
        {
            if (!patientInfo.IsNullExpresser && !donorInfo.IsNullExpresser)
            {
                return new ExpressingAlleleGradingCalculator();
            }

            if (patientInfo.IsNullExpresser && donorInfo.IsNullExpresser)
            {
                return new NullAlleleGradingCalculator();
            }

            return new ExpressingVsNullAlleleGradingCalculator();
        }

        private static bool IsSingleNullAllele(IHlaScoringInfo scoringInfo)
        {
            return scoringInfo is SingleAlleleScoringInfo info && info.IsNullExpresser;
        }
    }
}