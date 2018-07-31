using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

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

        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            return MatchGrade.Mismatch;
        }
    }
}