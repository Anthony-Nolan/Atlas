using Nova.SearchAlgorithm.Client.Models.SearchResults;
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

    public class SerologyGradingCalculator:
        GradingCalculatorBase,
        ISerologyGradingCalculator
    {
        protected override bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo, 
            IHlaScoringInfo donorInfo)
        {
            return patientInfo is SerologyScoringInfo ||
                   donorInfo is SerologyScoringInfo;
        }

        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult)
        {
            throw new System.NotImplementedException();
        }
    }
}