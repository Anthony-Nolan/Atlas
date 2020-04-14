using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Atlas.MatchingAlgorithm.Services.Scoring.Grading
{
    public interface IGradingCalculator
    {
        MatchGrade CalculateGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult);
    }
}
