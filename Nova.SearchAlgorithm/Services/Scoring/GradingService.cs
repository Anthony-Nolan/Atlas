using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IGradingService
    {
        PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults
        );
    }

    public class GradingService : IGradingService
    {
        public PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults
        )
        {
            // TODO: NOVA-1446: Implement
            throw new System.NotImplementedException();
        }
    }
}