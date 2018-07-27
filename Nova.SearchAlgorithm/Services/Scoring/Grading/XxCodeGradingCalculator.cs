using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when both typings are molecular, and at least
    /// one is an XX code.
    /// </summary>
    public interface IConsolidatedMolecularGradingCalculator : IGradingCalculator
    {
    }

    public class ConsolidatedMolecularGradingCalculator: IConsolidatedMolecularGradingCalculator
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