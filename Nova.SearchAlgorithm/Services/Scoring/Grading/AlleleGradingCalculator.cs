using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when comparing two original allele typings.
    /// </summary>
    public interface IAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class AlleleGradingCalculator: IAlleleGradingCalculator
    {
        public MatchGrade CalculateGrade(
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult)
        {
            // TODO: NOVA-1446 - Implement
            return MatchGrade.NotCalculated;
        }
    }
}