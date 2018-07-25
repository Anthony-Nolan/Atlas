using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when at least one typing is serology;
    /// the other typing can be either serology or molecular.
    /// </summary>
    public interface ISerologyGradingCalculator : IGradingCalculator
    {
    }

    public class SerologyGradingCalculator: ISerologyGradingCalculator
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