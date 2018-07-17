using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Confidence
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades);
    }

    public class ConfidenceService : IConfidenceService
    {
        private readonly IConfidenceCalculator confidenceCalculator;

        public ConfidenceService(IConfidenceCalculator confidenceCalculator)
        {
            this.confidenceCalculator = confidenceCalculator;
        }
        
        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            return patientLookupResults.Map((locus, position, lookupResult) =>
            {
                var matchGradesAtPosition = matchGrades.DataAtPosition(locus, position);
                var confidences = matchGradesAtPosition.Orientations.Select(o =>
                    CalculateConfidenceForOrientation(locus, position, lookupResult, donorLookupResults, o));
                return confidences.Max();
            });
        }

        private MatchConfidence CalculateConfidenceForOrientation(
            Locus locus,
            TypePositions position,
            IHlaScoringLookupResult patientLookupResult,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            MatchOrientation matchOrientation)
        {
            switch (matchOrientation)
            {
                case MatchOrientation.Direct:
                    return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, position));
                case MatchOrientation.Cross:
                    switch (position)
                    {
                        case TypePositions.One:
                            return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.Two));
                        case TypePositions.Two:
                            return confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.One));
                        case TypePositions.None:
                        case TypePositions.Both:
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchOrientation), matchOrientation, null);
            }
        }
    }
}