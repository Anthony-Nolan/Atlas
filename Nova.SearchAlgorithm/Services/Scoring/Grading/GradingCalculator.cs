using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public interface IGradingCalculator
    {
        MatchGrade CalculateGrading(
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult);
    }

    public class GradingCalculator: IGradingCalculator
    {
        public MatchGrade CalculateGrading(
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult)
        {
            // TODO: NOVA-1446 - Implement
            return MatchGrade.Mismatch;
        }
    }
}