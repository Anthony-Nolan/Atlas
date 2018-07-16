using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Spatial;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades
        );
    }

    public class ConfidenceService : IConfidenceService
    {
        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<MatchGradeResult> matchGrades
        )
        {
            return patientLookupResults.Map<MatchConfidence>((locus, position, lookupResult) =>
            {
                var matchGradesAtPosition = matchGrades.DataAtPosition(locus, position);
                var confidences = matchGradesAtPosition.Orientations.Select(o => CalculateConfidenceForOrientation(locus, position, lookupResult, donorLookupResults, o));
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
                    return CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, position));
                case MatchOrientation.Cross:
                    switch (position)
                    {
                        case TypePositions.One:
                            return CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.Two));
                        case TypePositions.Two:
                            return CalculateConfidence(patientLookupResult, donorLookupResults.DataAtPosition(locus, TypePositions.One));
                        case TypePositions.None:
                        case TypePositions.Both:
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchOrientation), matchOrientation, null);
            }
        }

        private MatchConfidence CalculateConfidence(IHlaScoringLookupResult patientLookupResult, IHlaScoringLookupResult donorLookupResult)
        {
            return MatchConfidence.Mismatch;
        }
    }
}