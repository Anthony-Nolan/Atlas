using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when comparing two original allele typings.
    /// </summary>
    public interface ISingleAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class SingleAlleleGradingCalculator :
        GradingCalculatorBase,
        ISingleAlleleGradingCalculator
    {
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

            var alleleGradingCalculator = GetAlleleGradingCalculator(patientInfo, donorInfo);

            return alleleGradingCalculator.GetMatchGrade(patientInfo, donorInfo);
        }

        private static AlleleGradingCalculatorBase GetAlleleGradingCalculator(
            AlleleGradingInfo patientInfo, 
            AlleleGradingInfo donorInfo)
        {
            if (!patientInfo.Allele.IsNullExpresser && !donorInfo.Allele.IsNullExpresser)
            {
                return new ExpressingAlleleGradingCalculator();
            }

            if (patientInfo.Allele.IsNullExpresser && donorInfo.Allele.IsNullExpresser)
            {
                return new NullAlleleGradingCalculator();
            }

            return new ExpressingVsNullAlleleGradingCalculator();
        }
    }
}