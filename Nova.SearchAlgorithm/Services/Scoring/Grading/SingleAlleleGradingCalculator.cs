using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;

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
        private enum ExpressionStatus
        {
            BothExpressing,
            BothNull,
            OneExpressingOneNull
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

            var expressionStatus = GetExpressionStatus(patientInfo, donorInfo);

            switch (expressionStatus)
            {
                case ExpressionStatus.OneExpressingOneNull:
                    return MatchGrade.Mismatch;
                case ExpressionStatus.BothExpressing:
                    return new ExpressingAlleleGradingCalculator().GetExpressingVsExpressingMatchGrade(patientInfo, donorInfo);
                case ExpressionStatus.BothNull:
                default:
                    throw new ArgumentOutOfRangeException($"Cannot grade expression status: {expressionStatus}.");
            }
        }

        private static ExpressionStatus GetExpressionStatus(
            AlleleGradingInfo patientInfo, 
            AlleleGradingInfo donorInfo)
        {
            if (!patientInfo.Allele.IsNullExpresser == !donorInfo.Allele.IsNullExpresser)
            {
                return ExpressionStatus.BothExpressing;
            }

            if (patientInfo.Allele.IsNullExpresser == donorInfo.Allele.IsNullExpresser)
            {
                return ExpressionStatus.BothNull;
            }

            return ExpressionStatus.OneExpressingOneNull;
        }
    }
}