using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IEnumerable<IHlaScoringLookupResult>> donorLookupResults,
            PhenotypeInfo<IEnumerable<IHlaScoringLookupResult>> patientLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades
        );
    }

    public class ConfidenceService : IConfidenceService
    {
        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IEnumerable<IHlaScoringLookupResult>> donorLookupResults,
            PhenotypeInfo<IEnumerable<IHlaScoringLookupResult>> patientLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades
        )
        {
            // TODO: NOVA-1447: Implement
            throw new NotImplementedException();
        }
    }
}